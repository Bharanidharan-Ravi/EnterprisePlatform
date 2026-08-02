using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Hooks;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 2: Context Enrichment. Populates AuthorizationContext (current user, tenant,
/// resolved effective permissions) and fires OnBeforeEvaluate hooks. Never validates.
/// </summary>
public sealed class ContextEnrichmentStage : IAuthorizationStage
{
    private readonly IAuthorizationContextFactory _contextFactory;
    private readonly IPermissionResolver _permissionResolver;
    private readonly AuthorizationHookInvoker _hooks;

    public ContextEnrichmentStage(
        IAuthorizationContextFactory contextFactory,
        IPermissionResolver permissionResolver,
        AuthorizationHookInvoker hooks)
    {
        _contextFactory = contextFactory;
        _permissionResolver = permissionResolver;
        _hooks = hooks;
    }

    public async Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateAsync(state.Request, cancellationToken);
        context.EffectivePermissions = await _permissionResolver.ResolveAsync(context.TenantId, context.UserId, cancellationToken);

        state.Context = context;

        await _hooks.RaiseBeforeEvaluateAsync(context, cancellationToken);
    }
}
