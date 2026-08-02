using System.Collections.Concurrent;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Stores;

/// <summary>
/// ⚠ DEVELOPMENT / TESTING / REFERENCE IMPLEMENTATION ONLY — not durable, not distributed-safe,
/// process-lifetime only. Registered as the default IRoleStore purely so Rbac is runnable
/// out-of-the-box (Test Harness, early development). A production deployment MUST supply its
/// own IRoleStore (typically backed by APIPlatform.Data) and register it BEFORE calling
/// AddRbac() — ServiceCollectionExtensions uses TryAddSingleton throughout specifically so an
/// app-supplied registration always wins over this one. Rbac itself never references Data
/// directly (see the architecture review's Dependency Graph section).
/// </summary>
public sealed class InMemoryRoleStore : IRoleStore
{
    private readonly ConcurrentDictionary<string, Role> _roles = new();
    private readonly ConcurrentBag<(string TenantId, string UserId, string RoleId)> _userRoles = new();
    private readonly ConcurrentBag<PermissionGrant> _grants = new();
    private readonly ConcurrentDictionary<string, List<PolicyRule>> _policyRulesByTenant = new();

    public void SeedRole(Role role) => _roles[role.Id] = role;

    public Task<IReadOnlyCollection<Role>> GetEffectiveRolesForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var directRoleIds = _userRoles
            .Where(ur => ur.TenantId == tenantId && ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToHashSet();

        var expanded = new HashSet<string>(directRoleIds);
        var frontier = new Queue<string>(directRoleIds);

        while (frontier.Count > 0)
        {
            var roleId = frontier.Dequeue();
            if (_roles.TryGetValue(roleId, out var role) && role.ParentRoleId is { } parentId && expanded.Add(parentId))
            {
                frontier.Enqueue(parentId);
            }
        }

        IReadOnlyCollection<Role> result = expanded
            .Select(id => _roles.TryGetValue(id, out var r) ? r : null)
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForRolesAsync(string tenantId, IEnumerable<string> roleIds, CancellationToken cancellationToken = default)
    {
        var roleIdSet = roleIds.ToHashSet();
        IReadOnlyCollection<PermissionGrant> result = _grants
            .Where(g => g.TenantId == tenantId && g.RoleId is not null && roleIdSet.Contains(g.RoleId))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PermissionGrant> result = _grants
            .Where(g => g.TenantId == tenantId && g.UserId == userId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<PolicyRule>> GetPolicyRulesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PolicyRule> result = _policyRulesByTenant.TryGetValue(tenantId, out var list)
            ? list
            : Array.Empty<PolicyRule>();
        return Task.FromResult(result);
    }

    public Task AssignRoleAsync(string tenantId, string userId, string roleId, CancellationToken cancellationToken = default)
    {
        _userRoles.Add((tenantId, userId, roleId));
        return Task.CompletedTask;
    }

    public Task GrantPermissionAsync(PermissionGrant grant, CancellationToken cancellationToken = default)
    {
        _grants.Add(grant);
        return Task.CompletedTask;
    }

    public Task RegisterPolicyRuleAsync(string tenantId, PolicyRule rule, CancellationToken cancellationToken = default)
    {
        var list = _policyRulesByTenant.GetOrAdd(tenantId, _ => new List<PolicyRule>());
        lock (list) { list.Add(rule); }
        return Task.CompletedTask;
    }
}
