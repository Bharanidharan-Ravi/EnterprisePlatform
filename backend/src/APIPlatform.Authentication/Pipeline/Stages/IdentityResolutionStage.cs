using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 1 — resolves who is trying to authenticate. No password verification,
/// no token generation, no validation. Only identity lookup.</summary>
public sealed class IdentityResolutionStage : IAuthenticationStage
{
    private readonly IIdentityResolver _resolver;

    public IdentityResolutionStage(IIdentityResolver resolver) => _resolver = resolver;

    public async Task ExecuteAsync(AuthenticationContext context)
    {
        context.User = await _resolver.ResolveAsync(
            context.Request.LoginIdentifier,
            context.Request.TenantId,
            context.CancellationToken);

        context.ResolvedIdentityProvider = "local";
    }
}
