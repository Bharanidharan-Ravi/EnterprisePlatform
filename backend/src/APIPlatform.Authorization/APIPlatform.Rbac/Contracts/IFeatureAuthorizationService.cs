namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// Permission-based feature gate only. Does NOT consult environment/rollout flags — that
/// composition point (APIPlatform.FeatureManagement, Master Plan Step 17) is deliberately not
/// referenced here, since Rbac (Step 5) must not take a forward dependency on a module that
/// doesn't exist yet. See DefaultAuthorizationContextFactory remarks for the reserved seam.
/// </summary>
public interface IFeatureAuthorizationService
{
    Task<bool> IsFeatureAllowedAsync(string featureKey, CancellationToken cancellationToken = default);
}
