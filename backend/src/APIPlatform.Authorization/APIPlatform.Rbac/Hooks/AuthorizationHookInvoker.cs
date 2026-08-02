using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Hooks;

/// <summary>Broadcasts hook callbacks to every registered IAuthorizationHook.</summary>
public sealed class AuthorizationHookInvoker
{
    private readonly IEnumerable<IAuthorizationHook> _hooks;

    public AuthorizationHookInvoker(IEnumerable<IAuthorizationHook> hooks) => _hooks = hooks;

    public async Task RaiseBeforeEvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken)
    {
        foreach (var hook in _hooks)
            await hook.OnBeforeEvaluateAsync(context, cancellationToken);
    }

    public async Task RaiseAfterEvaluateAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken)
    {
        foreach (var hook in _hooks)
            await hook.OnAfterEvaluateAsync(context, result, cancellationToken);
    }

    public async Task RaiseDeniedAsync(AuthorizationContext context, AuthorizationResult result, CancellationToken cancellationToken)
    {
        foreach (var hook in _hooks)
            await hook.OnDeniedAsync(context, result, cancellationToken);
    }
}
