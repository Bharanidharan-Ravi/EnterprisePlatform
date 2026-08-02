using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 4: Planning. Determines HOW evaluation will occur — here, that means building the
/// ordered list of applicable PolicyRules for the required permission key(s). No execution.
/// </summary>
public sealed class PlanningStage : IAuthorizationStage
{
    public Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var permissions = state.Context!.EffectivePermissions!;

        state.ApplicablePolicies = permissions.PolicyRules
            .Where(p => state.RequiredPermissionKeys.Contains(p.PermissionKey, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Priority)
            .ToList();

        return Task.CompletedTask;
    }
}
