using System.Security.Claims;

namespace APIPlatform.Authentication.Context;

/// <summary>Immutable implementation populated by CurrentUserContextMiddleware on every
/// authenticated request, and directly by AuthenticationExecutionStage after login.</summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; init; }
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public string? SessionId { get; init; }
    public string? TenantId { get; init; }
    public string? CompanyId { get; init; }
    public string? BranchId { get; init; }
    public string? DepartmentId { get; init; }
    public IReadOnlyList<string> RoleIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PermissionIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Claim> Claims { get; init; } = Array.Empty<Claim>();

    public string? GetClaim(string claimType) =>
        Claims.FirstOrDefault(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>Builds from a flat claim list — the single place where claim-type strings
    /// are known. Consumers never parse claims themselves.</summary>
    public static CurrentUserContext FromClaims(IReadOnlyList<Claim> claims)
    {
        string? Get(string type) =>
            claims.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase))?.Value;

        IReadOnlyList<string> GetAll(string type) =>
            claims.Where(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                  .Select(c => c.Value).ToList();

        return new CurrentUserContext
        {
            IsAuthenticated = true,
            UserId       = Get(ClaimTypes.NameIdentifier) ?? Get("sub") ?? string.Empty,
            Username     = Get(ClaimTypes.Name) ?? string.Empty,
            Email        = Get(ClaimTypes.Email),
            SessionId    = Get("sid"),
            TenantId     = Get("tenant_id"),
            CompanyId    = Get("company_id"),
            BranchId     = Get("branch_id"),
            DepartmentId = Get("department_id"),
            RoleIds      = GetAll(ClaimTypes.Role),
            PermissionIds= GetAll("permission"),
            Claims       = claims
        };
    }

    public static ICurrentUserContext Anonymous { get; } = new AnonymousUserContext();

    private sealed class AnonymousUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;
        public string UserId => string.Empty;
        public string Username => string.Empty;
        public string? Email => null;
        public string? SessionId => null;
        public string? TenantId => null;
        public string? CompanyId => null;
        public string? BranchId => null;
        public string? DepartmentId => null;
        public IReadOnlyList<string> RoleIds => Array.Empty<string>();
        public IReadOnlyList<string> PermissionIds => Array.Empty<string>();
        public IReadOnlyList<Claim> Claims => Array.Empty<Claim>();
        public string? GetClaim(string claimType) => null;
    }
}
