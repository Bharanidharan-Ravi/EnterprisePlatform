using APIPlatform.Rbac.Common;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 3: Validation. Structural/metadata validation only — is the request well-formed and
/// was context successfully built. NEVER performs a permission/policy decision (that's a
/// business/authorization outcome, not a structural one) and never executes operations.
/// </summary>
public sealed class ValidationStage : IAuthorizationStage
{
    public Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var request = state.Request;

        if (string.IsNullOrWhiteSpace(request.ResourceKey))
            throw new AuthorizationRequestException("AuthorizationRequest.ResourceKey is required.");

        if (string.IsNullOrWhiteSpace(request.Action))
            throw new AuthorizationRequestException("AuthorizationRequest.Action is required.");

        if (request.ResourceType == ResourceType.Field && string.IsNullOrWhiteSpace(request.FieldKey))
            throw new AuthorizationRequestException("AuthorizationRequest.FieldKey is required for Field-type requests.");

        if (state.Context is null)
            throw new AuthorizationRequestException("Authorization context was not populated before Validation Stage.");

        if (state.Context.EffectivePermissions is null)
            throw new AuthorizationRequestException("Effective permissions were not resolved before Validation Stage.");

        return Task.CompletedTask;
    }
}
