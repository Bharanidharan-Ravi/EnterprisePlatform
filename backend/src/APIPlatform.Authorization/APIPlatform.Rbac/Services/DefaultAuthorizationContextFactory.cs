using APIPlatform.Foundation.Interfaces;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Services;

/// <summary>
/// Builds AuthorizationContext from ICurrentUser + ITenantContext (both resolved via DI from
/// APIPlatform.Foundation — Rbac never references APIPlatform.Auth directly; Auth populates
/// ICurrentUser at runtime, Rbac only consumes the Foundation abstraction. This keeps the two
/// packages independently usable, per the Authn/Authz conceptual split, Master Plan Section 3.2),
/// plus IUserScopeStore for row/policy-scoping claims (department/branch/company) — sourced live,
/// per request, rather than from ICurrentUser.Claims/the JWT (see IUserScopeStore's own doc comment
/// for why a token snapshot isn't safe to enforce against). An app that registers a real
/// IUserScopeStore gets scope-aware Claims for free here; no factory override needed.
/// </summary>
public sealed class DefaultAuthorizationContextFactory : IAuthorizationContextFactory
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUserScopeStore _scopeStore;

    public DefaultAuthorizationContextFactory(ICurrentUser currentUser, ITenantContext tenantContext, IUserScopeStore scopeStore)
    {
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _scopeStore = scopeStore;
    }

    public async Task<AuthorizationContext> CreateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        // Foundation's ICurrentUser/ITenantContext expose UserId/TenantId as nullable (no
        // caller/tenant resolved yet is a valid state); AuthorizationContext keeps them required
        // non-null strings, so an unresolved caller/tenant maps to empty string rather than
        // widening AuthorizationContext's contract.
        var userId = _currentUser.UserId ?? string.Empty;
        var tenantId = _tenantContext.TenantId ?? string.Empty;

        // Scope values win over same-named ICurrentUser claims: the store is read fresh per
        // request and the claim may be a stale JWT snapshot — see IUserScopeStore's doc comment.
        var claims = new Dictionary<string, string>(_currentUser.Claims, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in await _scopeStore.GetScopesAsync(tenantId, userId, cancellationToken))
            claims[key] = value;

        return new AuthorizationContext
        {
            UserId = userId,
            TenantId = tenantId,
            Request = request,
            Claims = claims
        };
    }
}
