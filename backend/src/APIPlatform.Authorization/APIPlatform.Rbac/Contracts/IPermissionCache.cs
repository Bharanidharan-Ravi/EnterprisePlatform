using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// Caches resolved PermissionSets, tenant+user scoped. InvalidateAsync must be O(1) — no
/// distributed cache walking — see MemoryPermissionCache's version-bump key strategy.
/// </summary>
public interface IPermissionCache
{
    Task<PermissionSet?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task SetAsync(string tenantId, string userId, PermissionSet permissionSet, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    Task InvalidateAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
}
