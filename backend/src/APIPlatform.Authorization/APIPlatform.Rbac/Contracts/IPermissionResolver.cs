using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>Resolves the effective, cache-eligible PermissionSet for one (tenant, user) pair.</summary>
public interface IPermissionResolver
{
    Task<PermissionSet> ResolveAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
}
