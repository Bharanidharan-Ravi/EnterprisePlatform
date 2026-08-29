using System.Collections.Concurrent;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Stores;

/// <summary>
/// ⚠ DEVELOPMENT / TESTING / REFERENCE IMPLEMENTATION ONLY — not durable, not distributed-safe,
/// process-lifetime only. Registered as the default IUserScopeStore purely so Rbac is runnable
/// out-of-the-box; returns an empty scope set for every user until values are set, which combined
/// with a fail-closed row filter means "unconfigured" reads as "sees nothing", never "sees
/// everything". A production deployment MUST supply its own IUserScopeStore (typically backed by
/// APIPlatform.Data) and register it BEFORE calling AddRbac() — ServiceCollectionExtensions uses
/// TryAddSingleton throughout specifically so an app-supplied registration always wins over this one.
/// </summary>
public sealed class InMemoryUserScopeStore : IUserScopeStore
{
    private readonly ConcurrentDictionary<(string TenantId, string UserId, string ScopeKey), string> _scopes = new();

    public Task<IReadOnlyDictionary<string, string>> GetScopesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string> result = _scopes
            .Where(kv => kv.Key.TenantId == tenantId && kv.Key.UserId == userId)
            .ToDictionary(kv => kv.Key.ScopeKey, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(result);
    }

    public Task SetScopeAsync(string tenantId, string userId, string scopeKey, string scopeValue, CancellationToken cancellationToken = default)
    {
        _scopes[(tenantId, userId, scopeKey)] = scopeValue;
        return Task.CompletedTask;
    }
}
