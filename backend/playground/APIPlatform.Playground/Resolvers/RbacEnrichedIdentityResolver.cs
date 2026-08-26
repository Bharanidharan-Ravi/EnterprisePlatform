using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Playground.Resolvers;

/// <summary>
/// Decorates the app's real <see cref="IIdentityResolver"/> so every resolved <see cref="UserInfo"/>
/// carries live RoleIds/PermissionIds from APIPlatform.Rbac. Closes the gap flagged in
/// AuthenticationExtensions: neither <see cref="LoginsIdentityResolver"/> nor
/// <see cref="PlaygroundIdentityResolver"/> ever populates those two properties, so ClaimsBuilder
/// has always emitted a JWT with zero "role"/"permission" claims — meaning every frontend
/// PermissionGuard/RoleGuard/usePermission/useRole call has had nothing to read.
///
/// This does NOT change how the API itself enforces authorization: EmployeesController (and any
/// future controller) calls ICrudAuthorizationService directly, which re-resolves permissions
/// live from IRoleStore on every request — it never trusts the JWT. This decorator only affects
/// what the CLIENT is told about itself, so UI-level gates have real data to render against.
///
/// Deliberately lives here (Playground composition root), not inside APIPlatform.Authentication or
/// APIPlatform.Rbac — those two packages still never reference each other directly (same rule
/// HttpCurrentUserContextAdapter documents for ICurrentUser/ITenantContext). Registration
/// requires AddRbac() to already be in the container by build time; AddEmployeeModule() in
/// Program.cs guarantees that (see AuthenticationExtensions.AddAPIPlatformAuthentication).
/// </summary>
public sealed class RbacEnrichedIdentityResolver : IIdentityResolver
{
    private readonly IIdentityResolver _inner;
    private readonly IRoleService _roleService;
    private readonly IPermissionResolver _permissionResolver;

    public RbacEnrichedIdentityResolver(IIdentityResolver inner, IRoleService roleService, IPermissionResolver permissionResolver)
    {
        _inner = inner;
        _roleService = roleService;
        _permissionResolver = permissionResolver;
    }

    public async Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _inner.ResolveAsync(loginIdentifier, tenantId, cancellationToken);
        return user is null ? null : await EnrichAsync(user, cancellationToken);
    }

    public async Task<UserInfo?> ResolveByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _inner.ResolveByIdAsync(userId, cancellationToken);
        return user is null ? null : await EnrichAsync(user, cancellationToken);
    }

    private async Task<UserInfo> EnrichAsync(UserInfo user, CancellationToken cancellationToken)
    {
        // Deliberately NOT user.TenantId. This host is single-tenant
        // (HttpCurrentUserContextAdapter.IsMultiTenant = false) and its ITenantContext.TenantId
        // — what DefaultAuthorizationContextFactory actually uses for every request-time RBAC
        // check — is hardcoded to TestTenantId and ignores the JWT's tenant_id claim entirely.
        // user.TenantId (from LoginsIdentityResolver's Dbname column, e.g. "IQS_DB") is real data
        // but not what enforcement keys off; using it here would enrich against a tenant no grant
        // was ever seeded under (verified: this was a real bug — Dbname is non-null in practice, so
        // an "only fall back when null" version of this line silently produced empty RoleIds/
        // PermissionIds for the actual Logins-backed login). A real multi-tenant deployment
        // replaces both this and HttpCurrentUserContextAdapter to agree on one tenant source.
        var tenantId = HttpCurrentUserContextAdapter.TestTenantId;

        var roles = await _roleService.GetRolesAsync(tenantId, user.UserId, cancellationToken);
        var permissions = await _permissionResolver.ResolveAsync(tenantId, user.UserId, cancellationToken);

        return new UserInfo
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            EmployeeCode = user.EmployeeCode,
            PasswordHash = user.PasswordHash,
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            FailedAttemptCount = user.FailedAttemptCount,
            LockedUntil = user.LockedUntil,
            PasswordExpiresAt = user.PasswordExpiresAt,
            TenantId = user.TenantId,
            CompanyId = user.CompanyId,
            BranchId = user.BranchId,
            DepartmentId = user.DepartmentId,
            RoleIds = roles.Select(r => r.Id).ToArray(),
            PermissionIds = permissions.AllowedKeys.ToArray(),
            ExtendedClaims = user.ExtendedClaims
        };
    }
}
