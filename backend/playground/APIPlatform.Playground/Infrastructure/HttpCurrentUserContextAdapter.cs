using APIPlatform.Authentication.Context;
using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.Playground.Infrastructure;

/// <summary>
/// Phase 2 wiring gap closed: nowhere in the platform did anything implement Foundation's
/// ICurrentUser/ITenantContext (constructor-injected by both CrudEngine&lt;T&gt; and Rbac's
/// DefaultAuthorizationContextFactory) from the real authenticated request. Authentication only
/// ever populates its own ICurrentUserContext (via CurrentUserContextMiddleware); nothing bridged
/// the two. This adapter is that bridge — application-level, not a platform change.
/// TenantId is intentionally fixed to a single constant: this test host is single-tenant
/// (IsMultiTenant = false), so RBAC seeding and runtime resolution always agree.
/// </summary>
public sealed class HttpCurrentUserContextAdapter : ICurrentUser, ITenantContext
{
    /// <summary>TEST ONLY — single fixed tenant for this Phase 2 validation host.</summary>
    public const string TestTenantId = "default";

    private readonly ICurrentUserContextAccessor _accessor;

    public HttpCurrentUserContextAdapter(ICurrentUserContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ICurrentUserContext Current => _accessor.Current;

    public string? UserId => Current.IsAuthenticated ? Current.UserId : null;

    public string? UserName => Current.IsAuthenticated ? Current.Username : null;

    public bool IsAuthenticated => Current.IsAuthenticated;

    public IReadOnlyDictionary<string, string> Claims =>
        Current.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value);

    public string? GetClaim(string claimType) => Current.GetClaim(claimType);

    public string? TenantId => IsAuthenticated ? TestTenantId : null;

    public string? TenantCode => null;

    public bool HasTenant => IsAuthenticated;

    public bool IsMultiTenant => false;
}
