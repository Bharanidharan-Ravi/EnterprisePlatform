namespace APIPlatform.Authentication.Models;

/// <summary>Immutable plan produced by AuthenticationPlanningStage and consumed by
/// AuthenticationExecutionStage. Adding a new strategy never touches the pipeline or stages —
/// only the planner and executor care about the strategy value.</summary>
public sealed class AuthenticationPlan
{
    public required AuthenticationStrategyType Strategy { get; init; }
    public required string? ExternalProviderId { get; init; }
    public required bool GenerateRefreshToken { get; init; }
    public required SessionMode SessionMode { get; init; }
}
