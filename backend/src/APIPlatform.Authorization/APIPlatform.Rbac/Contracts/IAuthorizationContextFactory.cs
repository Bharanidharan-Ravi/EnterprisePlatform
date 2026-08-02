using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

public interface IAuthorizationContextFactory
{
    Task<AuthorizationContext> CreateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
}
