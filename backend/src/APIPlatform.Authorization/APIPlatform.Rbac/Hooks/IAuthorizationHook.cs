using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Hooks;

/// <summary>
/// Extension point mirroring CrudEngine's OnBefore/OnAfter convention (Master Plan Section
/// 7.2). Consuming apps register implementations via DI; all registered hooks are invoked for
/// every evaluation, so an implementation should check context.Request.ResourceType/ResourceKey
/// and no-op for requests it doesn't care about. (A future refinement could specialize hooks
/// per resource type via keyed DI services — not needed for the v1 Harness checkpoint.)
/// </summary>
public interface IAuthorizationHook
{
    Task OnBeforeEvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken);
    Task OnAfterEvaluateAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken);
    Task OnDeniedAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken);
}
