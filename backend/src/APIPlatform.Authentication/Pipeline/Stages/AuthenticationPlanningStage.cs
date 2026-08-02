using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 4 — decides how authentication will execute. Produces the immutable
/// AuthenticationPlan. No execution happens here.</summary>
public sealed class AuthenticationPlanningStage : IAuthenticationStage
{
    private readonly IAuthenticationPlanner _planner;

    public AuthenticationPlanningStage(IAuthenticationPlanner planner) => _planner = planner;

    public Task ExecuteAsync(AuthenticationContext context)
    {
        context.Plan = _planner.CreatePlan(context);
        return Task.CompletedTask;
    }
}
