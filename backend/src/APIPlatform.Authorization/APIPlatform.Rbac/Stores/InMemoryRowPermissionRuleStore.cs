using System.Collections.Concurrent;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Stores;

/// <summary>
/// ⚠ DEVELOPMENT / TESTING / REFERENCE IMPLEMENTATION ONLY — process-lifetime, not durable.
/// Replace via DI (register IRowPermissionRuleStore before AddRbac()) for production use.
/// </summary>
public sealed class InMemoryRowPermissionRuleStore : IRowPermissionRuleStore
{
    private readonly ConcurrentBag<(string TenantId, RowPermissionRule Rule)> _rules = new();

    public Task<IReadOnlyCollection<RowPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<RowPermissionRule> result = _rules
            .Where(r => r.TenantId == tenantId && r.Rule.EntityKey == entityKey)
            .Select(r => r.Rule)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddRuleAsync(string tenantId, RowPermissionRule rule, CancellationToken cancellationToken = default)
    {
        _rules.Add((tenantId, rule));
        return Task.CompletedTask;
    }
}
