using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// Persistence abstraction for roles and grants. Kept provider-independent — the default
/// registration wires an in-memory implementation; a real deployment supplies one backed by
/// APIPlatform.Data, without Rbac ever referencing Data directly.
/// </summary>
public interface IRoleStore
{
    Task<IReadOnlyCollection<Role>> GetEffectiveRolesForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForRolesAsync(string tenantId, IEnumerable<string> roleIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionGrant>> GetGrantsForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PolicyRule>> GetPolicyRulesAsync(string tenantId, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(string tenantId, string userId, string roleId, CancellationToken cancellationToken = default);
    Task GrantPermissionAsync(PermissionGrant grant, CancellationToken cancellationToken = default);
    Task RegisterPolicyRuleAsync(string tenantId, PolicyRule rule, CancellationToken cancellationToken = default);
}
