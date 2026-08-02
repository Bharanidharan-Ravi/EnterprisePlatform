using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Pipeline;
using APIPlatform.Rbac.Pipeline.Stages;

namespace APIPlatform.Rbac.Services;

/// <summary>
/// Runs the six pipeline stages in the fixed EnterprisePlatform Standard Execution order.
/// Stages are injected as concrete types (not IEnumerable&lt;IAuthorizationStage&gt;) so
/// ordering is explicit and compiler-checked, not dependent on DI registration order — a
/// deliberate choice for 10+ year maintainability over the more "generic" collection approach.
/// </summary>
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    private readonly PermissionResolutionStage _resolution;
    private readonly ContextEnrichmentStage _enrichment;
    private readonly ValidationStage _validation;
    private readonly PlanningStage _planning;
    private readonly ExecutionStage _execution;
    private readonly ResponseMappingStage _responseMapping;

    public PermissionEvaluator(
        PermissionResolutionStage resolution,
        ContextEnrichmentStage enrichment,
        ValidationStage validation,
        PlanningStage planning,
        ExecutionStage execution,
        ResponseMappingStage responseMapping)
    {
        _resolution = resolution;
        _enrichment = enrichment;
        _validation = validation;
        _planning = planning;
        _execution = execution;
        _responseMapping = responseMapping;
    }

    public async Task<AuthorizationResult> EvaluateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var state = new AuthorizationPipelineState { Request = request };

        await _resolution.ExecuteAsync(state, cancellationToken);       // Permission Resolution Stage
        await _enrichment.ExecuteAsync(state, cancellationToken);       // Context Enrichment Stage
        await _validation.ExecuteAsync(state, cancellationToken);       // Validation Stage
        await _planning.ExecuteAsync(state, cancellationToken);         // Planning Stage
        await _execution.ExecuteAsync(state, cancellationToken);        // Execution Stage
        await _responseMapping.ExecuteAsync(state, cancellationToken);  // Response Mapping Stage

        return state.Result!;
    }
}
