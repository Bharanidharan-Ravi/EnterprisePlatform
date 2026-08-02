using System.Security.Claims;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Claims;

/// <summary>Builds standard JWT claims from AuthenticationContext. App-specific claims are
/// added via IClaimsBuilderExtension — register one or more, they all run without modifying
/// this class.</summary>
public sealed class ClaimsBuilder : IClaimsBuilder
{
    private readonly IEnumerable<IClaimsBuilderExtension> _extensions;

    public ClaimsBuilder(IEnumerable<IClaimsBuilderExtension> extensions) => _extensions = extensions;

    public IReadOnlyList<Claim> Build(AuthenticationContext context)
    {
        var user = context.User!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Name, user.Username),
            new("sub", user.UserId),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(user.Email))       claims.Add(new(ClaimTypes.Email, user.Email));
        if (!string.IsNullOrEmpty(user.TenantId))    claims.Add(new("tenant_id", user.TenantId));
        if (!string.IsNullOrEmpty(user.CompanyId))   claims.Add(new("company_id", user.CompanyId));
        if (!string.IsNullOrEmpty(user.BranchId))    claims.Add(new("branch_id", user.BranchId));
        if (!string.IsNullOrEmpty(user.DepartmentId))claims.Add(new("department_id", user.DepartmentId));
        if (context.SessionId is not null)           claims.Add(new("sid", context.SessionId));

        foreach (var roleId in user.RoleIds)
            claims.Add(new(ClaimTypes.Role, roleId));

        foreach (var permId in user.PermissionIds)
            claims.Add(new("permission", permId));

        foreach (var (k, v) in user.ExtendedClaims)
            claims.Add(new(k, v));

        // Extension point — app-specific claims (DbName, AppVersion, etc.)
        foreach (var ext in _extensions)
            claims.AddRange(ext.Extend(context));

        return claims;
    }
}
