using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

public interface IRoleService
{
    Task<IReadOnlyCollection<Role>> GetRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string tenantId, string userId, string roleId, CancellationToken cancellationToken = default);
    Task GrantPermissionAsync(PermissionGrant grant, CancellationToken cancellationToken = default);
}
