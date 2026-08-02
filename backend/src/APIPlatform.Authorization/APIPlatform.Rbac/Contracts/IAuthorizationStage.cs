using APIPlatform.Rbac.Pipeline;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// One stage of the EnterprisePlatform Standard Execution Flow. Every module's pipeline
/// implements this shape; only the first stage's name/responsibility differs (here:
/// Permission Resolution). See docs/EnterprisePlatform_Execution_Standard.md.
/// </summary>
public interface IAuthorizationStage
{
    Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken);
}
