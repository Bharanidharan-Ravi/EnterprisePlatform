using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class FeatureAuthorizationService : IFeatureAuthorizationService
{
    private readonly IPermissionEvaluator _evaluator;

    public FeatureAuthorizationService(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public async Task<bool> IsFeatureAllowedAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        var request = new AuthorizationRequest
        {
            ResourceType = ResourceType.Feature,
            ResourceKey = featureKey,
            Action = "Use"
        };

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);
        return result.Allowed;
    }
}
