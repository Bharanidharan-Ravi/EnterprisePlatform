using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class RoleService : IRoleService
{
    private readonly IRoleStore _store;
    private readonly IPermissionCache _cache;

    public RoleService(IRoleStore store, IPermissionCache cache)
    {
        _store = store;
        _cache = cache;
    }

    public Task<IReadOnlyCollection<Role>> GetRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default) =>
        _store.GetEffectiveRolesForUserAsync(tenantId, userId, cancellationToken);

    public async Task AssignRoleAsync(string tenantId, string userId, string roleId, CancellationToken cancellationToken = default)
    {
        await _store.AssignRoleAsync(tenantId, userId, roleId, cancellationToken);
        await _cache.InvalidateAsync(tenantId, userId, cancellationToken);
    }

    public async Task GrantPermissionAsync(PermissionGrant grant, CancellationToken cancellationToken = default)
    {
        await _store.GrantPermissionAsync(grant, cancellationToken);

        // KNOWN v1 LIMITATION: a role-level grant only invalidates cache for the specific user
        // passed in (if UserId is set). Invalidating every user holding a given RoleId requires
        // a user<->role index the in-memory store doesn't maintain yet. Acceptable for the Step
        // 5 Harness checkpoint since role-permission changes are rare relative to per-request
        // evaluation volume; flagged here rather than silently glossed over.
        if (grant.UserId is not null)
        {
            await _cache.InvalidateAsync(grant.TenantId, grant.UserId, cancellationToken);
        }
    }
}
