using APIPlatform.Foundation;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Services;

/// <summary>
/// Builds AuthorizationContext from ICurrentUser + ITenantContext (both resolved via DI from
/// APIPlatform.Foundation — Rbac never references APIPlatform.Auth directly; Auth populates
/// ICurrentUser at runtime, Rbac only consumes the Foundation abstraction. This keeps the two
/// packages independently usable, per the Authn/Authz conceptual split, Master Plan Section 3.2).
/// </summary>
public sealed class DefaultAuthorizationContextFactory : IAuthorizationContextFactory
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;

    public DefaultAuthorizationContextFactory(ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public Task<AuthorizationContext> CreateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var context = new AuthorizationContext
        {
            UserId = _currentUser.UserId,
            TenantId = _tenantContext.TenantId,
            Request = request
        };

        return Task.FromResult(context);
    }
}
