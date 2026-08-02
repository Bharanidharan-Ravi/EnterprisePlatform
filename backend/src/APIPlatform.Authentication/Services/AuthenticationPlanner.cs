using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Services;

/// <summary>Default IAuthenticationPlanner. V1 always resolves to Local strategy.
/// Future: inspect context.Request.Extra["provider"] or tenant config to pick External,
/// OAuth, LDAP etc. — only this class changes, pipeline untouched.</summary>
public sealed class AuthenticationPlanner : IAuthenticationPlanner
{
    public AuthenticationPlan CreatePlan(AuthenticationContext context) => new()
    {
        Strategy            = AuthenticationStrategyType.Local,
        ExternalProviderId  = null,
        GenerateRefreshToken = context.Settings?.RefreshTokenEnabled ?? true,
        SessionMode         = context.Settings?.SessionMode ?? SessionMode.Multi
    };
}
