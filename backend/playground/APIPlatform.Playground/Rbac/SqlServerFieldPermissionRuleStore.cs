using APIPlatform.Data.Execution;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Durable, SQL Server-backed <see cref="IFieldPermissionRuleStore"/> — replaces APIPlatform.Rbac's
/// InMemoryFieldPermissionRuleStore, same reasoning as <see cref="SqlServerRowPermissionRuleStore"/>:
/// a field-mask rule decides whether sensitive data (e.g. Email) leaves the API at all, so losing
/// it on restart must not fail open (no rule = "no additional restriction" per
/// <see cref="FieldPermissionRule"/>'s own doc comment = the field becomes visible to everyone).
/// Same Singleton + IServiceScopeFactory shape, same idempotent writes, registered before AddRbac().
/// </summary>
public sealed class SqlServerFieldPermissionRuleStore : IFieldPermissionRuleStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqlServerFieldPermissionRuleStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithExecutorAsync<T>(Func<IDatabaseExecutor, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IDatabaseExecutor>();
        return await action(executor);
    }

    public Task<IReadOnlyCollection<FieldPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(async executor =>
        {
            var rows = await executor.QueryAsync<RuleRow>(
                @"SELECT [FieldKey], [PermissionKey], [Access]
                  FROM [RbacFieldPermissionRules]
                  WHERE [TenantId] = @TenantId AND [EntityKey] = @EntityKey",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["EntityKey"] = entityKey },
                cancellationToken: cancellationToken);

            return (IReadOnlyCollection<FieldPermissionRule>)rows.Select(r => r.ToModel(entityKey)).ToList();
        });

    public Task AddRuleAsync(string tenantId, FieldPermissionRule rule, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"IF NOT EXISTS (
                  SELECT 1 FROM [RbacFieldPermissionRules]
                  WHERE [TenantId] = @TenantId AND [EntityKey] = @EntityKey AND [FieldKey] = @FieldKey AND [PermissionKey] = @PermissionKey)
              INSERT INTO [RbacFieldPermissionRules] ([Id], [TenantId], [EntityKey], [FieldKey], [PermissionKey], [Access])
              VALUES (@Id, @TenantId, @EntityKey, @FieldKey, @PermissionKey, @Access)",
            new Dictionary<string, object?>
            {
                ["Id"] = Guid.NewGuid(),
                ["TenantId"] = tenantId,
                ["EntityKey"] = rule.EntityKey,
                ["FieldKey"] = rule.FieldKey,
                ["PermissionKey"] = rule.PermissionKey,
                ["Access"] = rule.Access.ToString()
            },
            cancellationToken: cancellationToken));

    private sealed class RuleRow
    {
        public string FieldKey { get; set; } = string.Empty;
        public string PermissionKey { get; set; } = string.Empty;
        public string Access { get; set; } = string.Empty;

        public FieldPermissionRule ToModel(string entityKey) => new()
        {
            EntityKey = entityKey,
            FieldKey = FieldKey,
            PermissionKey = PermissionKey,
            Access = Enum.Parse<FieldAccess>(Access)
        };
    }
}
