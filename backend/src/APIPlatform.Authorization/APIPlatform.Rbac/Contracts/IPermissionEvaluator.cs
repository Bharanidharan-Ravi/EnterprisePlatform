using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// The single public orchestration entry point for the whole Rbac pipeline. Every facade
/// service (Menu/Field/Row/Feature/Crud authorization) calls through this — there is exactly
/// one pipeline, not one per resource type.
/// </summary>
public interface IPermissionEvaluator
{
    Task<AuthorizationResult> EvaluateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
}
