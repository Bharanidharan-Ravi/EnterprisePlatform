using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Hooks;

/// <summary>
/// Extension point (Req 9) letting Audit/Workflow/Notification-type concerns observe or react to
/// CRUD execution without CrudEngine referencing them. Register multiple implementations via
/// DI — all run, in registration order, around every operation; each hook inspects
/// context.Operation to decide whether it cares (e.g. "only Before Insert").
/// </summary>
public interface ICrudPipelineHook
{
    Task OnBeforeAsync<TEntity>(CrudContext<TEntity> context) where TEntity : class;
    Task OnAfterAsync<TEntity>(CrudContext<TEntity> context) where TEntity : class;
}

/// <summary>Convenience base — override only the phases you care about.</summary>
public abstract class CrudPipelineHookBase : ICrudPipelineHook
{
    public virtual Task OnBeforeAsync<TEntity>(CrudContext<TEntity> context) where TEntity : class => Task.CompletedTask;
    public virtual Task OnAfterAsync<TEntity>(CrudContext<TEntity> context) where TEntity : class => Task.CompletedTask;
}
