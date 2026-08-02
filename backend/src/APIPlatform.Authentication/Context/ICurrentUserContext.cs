using System.Security.Claims;

namespace APIPlatform.Authentication.Context;

/// <summary>
/// Standard identity abstraction for the entire EnterprisePlatform.
/// Every future module (Authorization, Audit, Workflow, Notification, etc.) depends
/// on this interface only — never on HttpContext, ClaimsPrincipal, or JWT directly.
/// Authentication is the only module responsible for populating this.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string Username { get; }
    string? Email { get; }
    string? SessionId { get; }
    string? TenantId { get; }
    string? CompanyId { get; }
    string? BranchId { get; }
    string? DepartmentId { get; }
    IReadOnlyList<string> RoleIds { get; }
    IReadOnlyList<string> PermissionIds { get; }
    IReadOnlyList<Claim> Claims { get; }
    /// <summary>Reads any claim value by type — avoids raw Claims enumeration in consumers.</summary>
    string? GetClaim(string claimType);
}
