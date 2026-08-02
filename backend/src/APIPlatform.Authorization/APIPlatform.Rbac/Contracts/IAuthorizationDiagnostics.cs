using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// PLACEHOLDER — reserved extension point for future Authorization Diagnostics (tracing,
/// decision auditing, cache hit/miss counters, evaluation latency). Intentionally NOT
/// implemented and NOT registered in ServiceCollectionExtensions.AddRbac() — this is a
/// contract reservation only, per this review's scope ("no runtime logic required").
///
/// When implemented, an adapter is expected to be wired into the pipeline stages (most likely
/// ExecutionStage and ResponseMappingStage) via constructor injection, exactly like
/// AuthorizationHookInvoker — never by adding tracing calls scattered ad hoc through the
/// pipeline. Until then, no stage references this interface.
/// </summary>
public interface IAuthorizationDiagnostics
{
    void RecordEvaluation(AuthorizationContext context, AuthorizationResult result);
}
