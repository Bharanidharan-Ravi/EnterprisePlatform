using System.Collections.Concurrent;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Stores;

/// <summary>
/// ⚠ DEVELOPMENT / TESTING / REFERENCE IMPLEMENTATION ONLY — process-lifetime, not durable.
/// Replace via DI (register IFieldPermissionRuleStore before AddRbac()) for production use,
/// typically backed by APIPlatform.Data. Absence of a rule for a field means "no additional
/// restriction beyond the entity-level Crud permission" (see FieldPermissionRule remarks) —
/// this store simply returns an empty collection until rules are added.
/// </summary>
public sealed class InMemoryFieldPermissionRuleStore : IFieldPermissionRuleStore
{
    private readonly ConcurrentBag<(string TenantId, FieldPermissionRule Rule)> _rules = new();

    public Task<IReadOnlyCollection<FieldPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<FieldPermissionRule> result = _rules
            .Where(r => r.TenantId == tenantId && r.Rule.EntityKey == entityKey)
            .Select(r => r.Rule)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddRuleAsync(string tenantId, FieldPermissionRule rule, CancellationToken cancellationToken = default)
    {
        _rules.Add((tenantId, rule));
        return Task.CompletedTask;
    }
}
