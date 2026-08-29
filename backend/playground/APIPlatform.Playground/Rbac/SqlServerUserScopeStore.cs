using APIPlatform.Data.Execution;
using APIPlatform.Rbac.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Durable <see cref="IUserScopeStore"/> over [RbacUserScopes] (created by
/// <see cref="RbacRowScopeSqlServerMigration"/>) — replaces APIPlatform.Rbac's default
/// InMemoryUserScopeStore, whose own doc comment flags it "not durable, process-lifetime only."
/// Same Singleton + IServiceScopeFactory shape as <see cref="SqlServerRoleStore"/>, for the same
/// reason: it is consumed from a Singleton (RbacEnrichedIdentityResolver, indirectly, and Rbac's own
/// DefaultAuthorizationContextFactory) as well as per-request, and IDatabaseExecutor is Scoped.
/// Registered before AddRbac() so TryAddSingleton's default is skipped — see IUserScopeStore's own
/// doc comment for why request-time enforcement reads this instead of the JWT.
///
/// <b>Deliberately not cached.</b> PermissionResolver caches its PermissionSet for 5 minutes and
/// Phase 7 already owns the "invalidate that cache properly" problem; adding a second,
/// independently-stale cache here would mean a user's department could disagree with their
/// permissions for minutes at a time, for one indexed PK seek per row-scoped request. If this ever
/// shows up in a profile, cache it alongside the PermissionSet, not separately.
/// </summary>
public sealed class SqlServerUserScopeStore : IUserScopeStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqlServerUserScopeStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithExecutorAsync<T>(Func<IDatabaseExecutor, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IDatabaseExecutor>();
        return await action(executor);
    }

    public Task<IReadOnlyDictionary<string, string>> GetScopesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());

        return WithExecutorAsync(async executor =>
        {
            var rows = await executor.QueryAsync<ScopeRow>(
                "SELECT [ScopeKey], [ScopeValue] FROM [RbacUserScopes] WHERE [TenantId] = @TenantId AND [UserId] = @UserId",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["UserId"] = userId },
                cancellationToken: cancellationToken);

            return (IReadOnlyDictionary<string, string>)rows
                .GroupBy(r => r.ScopeKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().ScopeValue, StringComparer.OrdinalIgnoreCase);
        });
    }

    public Task SetScopeAsync(string tenantId, string userId, string scopeKey, string scopeValue, CancellationToken cancellationToken = default) =>
        WithExecutorAsync(executor => executor.ExecuteAsync(
            @"UPDATE [RbacUserScopes] SET [ScopeValue] = @ScopeValue
              WHERE [TenantId] = @TenantId AND [UserId] = @UserId AND [ScopeKey] = @ScopeKey;
              IF @@ROWCOUNT = 0
              INSERT INTO [RbacUserScopes] ([TenantId], [UserId], [ScopeKey], [ScopeValue])
              VALUES (@TenantId, @UserId, @ScopeKey, @ScopeValue)",
            new Dictionary<string, object?>
            {
                ["TenantId"] = tenantId,
                ["UserId"] = userId,
                ["ScopeKey"] = scopeKey,
                ["ScopeValue"] = scopeValue
            },
            cancellationToken: cancellationToken));

    private sealed class ScopeRow
    {
        public string ScopeKey { get; set; } = string.Empty;
        public string ScopeValue { get; set; } = string.Empty;
    }
}
