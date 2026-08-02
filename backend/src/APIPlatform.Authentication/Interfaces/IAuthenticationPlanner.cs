using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Determines how authentication will execute and produces the immutable
/// AuthenticationPlan. Apps may replace this to add custom strategy resolution.</summary>
public interface IAuthenticationPlanner
{
    AuthenticationPlan CreatePlan(AuthenticationContext context);
}
