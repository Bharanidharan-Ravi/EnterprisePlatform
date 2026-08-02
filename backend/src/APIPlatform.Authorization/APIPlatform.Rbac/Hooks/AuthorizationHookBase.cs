using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Hooks;

/// <summary>Convenience base class so consuming apps only override the callback(s) they need.</summary>
public abstract class AuthorizationHookBase : IAuthorizationHook
{
    public virtual Task OnBeforeEvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task OnAfterEvaluateAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task OnDeniedAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken) => Task.CompletedTask;
}
