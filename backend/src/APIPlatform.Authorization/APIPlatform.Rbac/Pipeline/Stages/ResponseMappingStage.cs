using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Hooks;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 6: Response Mapping. Converts pipeline state into the framework's AuthorizationResult.
/// Fires OnAfterEvaluate (always) and OnDenied (if not allowed). No execution logic here.
/// </summary>
public sealed class ResponseMappingStage : IAuthorizationStage
{
    private readonly AuthorizationHookInvoker _hooks;

    public ResponseMappingStage(AuthorizationHookInvoker hooks) => _hooks = hooks;

    public async Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var result = new AuthorizationResult
        {
            Allowed = state.Decision ?? false,
            Reason = state.DenialReason,
            AppliedPolicies = state.ApplicablePolicies,
            RowFilter = state.RowFilter,
            FieldMask = state.FieldMask
        };

        state.Result = result;

        await _hooks.RaiseAfterEvaluateAsync(state.Context!, result, cancellationToken);

        if (!result.Allowed)
            await _hooks.RaiseDeniedAsync(state.Context!, result, cancellationToken);
    }
}
