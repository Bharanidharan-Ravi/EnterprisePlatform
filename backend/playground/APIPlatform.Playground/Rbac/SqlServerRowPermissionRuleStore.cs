using APIPlatform.Data.Execution;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Durable, SQL Server-backed <see cref="IRowPermissionRuleStore"/> — replaces APIPlatform.Rbac's
/// InMemoryRowPermissionRuleStore, whose own doc comment flags it "DEVELOPMENT / TESTING /
/// REFERENCE IMPLEMENTATION ONLY — process-lifetime, not durable."
///
/// Phase 2 decision (the phase's own "same durability question as Phase 1" step): durable, not
/// in-memory. A row-permission rule is the thing that decides whether a user sees another
/// department's data at all; losing it on restart fails OPEN (no rule = no filter = every row
/// visible), which is exactly the failure mode not worth carrying. Same registration/lifetime
/// pattern as Phase 0's <see cref="SqlServerRoleStore"/>: registered as a Singleton before
/// AddRbac() so its TryAddSingleton default is skipped, and it never constructor-injects the
/// Scoped IDatabaseExecutor — it opens a short DI scope per call instead.
///
/// Writes are idempotent (IF NOT EXISTS ... INSERT) because EmployeeModuleInitializationService
/// re-seeds on every app start.
/// </summary>
public sealed class SqlServerRowPermissionRuleStore : IRowPermissionRuleStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqlServerRowPermissionRuleStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithExecutorAsync<T>(Func<IDatabaseExecutor, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IDatabaseExecutor>();
        return await action(executor);
    }

    public Task<IReadOnlyCollection<RowPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var rows = await executor.QueryAsync<RuleRow>(
                @"SELECT [EntityKey], [FilterDelegateKey], [TenantScoped]
                  FROM [RbacRowPermissionRules]
                  WHERE [TenantId] = @TenantId AND [EntityKey] = @EntityKey",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["EntityKey"] = entityKey },
                cancellationToken: cancellationToken);

            return (IReadOnlyCollection<RowPermissionRule>)rows.Select(r => r.ToModel()).ToList();
        });

    public Task AddRuleAsync(string tenantId, RowPermissionRule rule, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (
                  SELECT 1 FROM [RbacRowPermissionRules]
                  WHERE [TenantId] = @TenantId AND [EntityKey] = @EntityKey AND [FilterDelegateKey] = @FilterDelegateKey)
              INSERT INTO [RbacRowPermissionRules] ([Id], [TenantId], [EntityKey], [FilterDelegateKey], [TenantScoped])
              VALUES (@Id, @TenantId, @EntityKey, @FilterDelegateKey, @TenantScoped)",
            new Dictionary<string, object?>
            {
                ["Id"] = Guid.NewGuid(),
                ["TenantId"] = tenantId,
                ["EntityKey"] = rule.EntityKey,
                ["FilterDelegateKey"] = rule.FilterDelegateKey,
                ["TenantScoped"] = rule.TenantScoped
            },
            cancellationToken: cancellationToken));

    // Mutable row shape for Dapper materialization — RowPermissionRule itself is init-only/required.
    private sealed class RuleRow
    {
        public string EntityKey { get; set; } = string.Empty;
        public string FilterDelegateKey { get; set; } = string.Empty;
        public bool TenantScoped { get; set; }

        public RowPermissionRule ToModel() => new()
        {
            EntityKey = EntityKey,
            FilterDelegateKey = FilterDelegateKey,
            TenantScoped = TenantScoped
        };
    }
}
