using APIPlatform.CrudEngine.Hooks;
using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Pipeline.Stages;
using APIPlatform.Foundation.Entities;

namespace APIPlatform.CrudEngine.Pipeline;

/// <summary>
/// Orchestrates the fixed enterprise execution sequence — this is the standard execution model
/// every future EnterprisePlatform module (Auth, Workflow, Audit, Rule Engine, Notifications)
/// integrates into. Contains no business/execution logic itself, only stage sequencing; each
/// stage owns exactly one responsibility and stages communicate solely through CrudContext.
///
///   MetadataResolutionStage → ContextEnrichmentStage → ValidationStage
///     → [hooks] → ExecutionPlanningStage → ExecutionStage → [hooks] → ResponseMappingStage
///
/// Hook placement matches the two extension points already needed today (pre-execution /
/// post-execution, e.g. Workflow / Notification). Adding a hook boundary at another stage seam
/// later is a one-line addition here — stages themselves never need to change.
/// </summary>
public sealed class CrudPipeline<TEntity> : ICrudPipeline<TEntity> where TEntity : class, IEntity
{
    private readonly MetadataResolutionStage<TEntity> _metadataStage;
    private readonly ContextEnrichmentStage<TEntity> _enrichmentStage;
    private readonly ValidationStage<TEntity> _validationStage;
    private readonly ExecutionPlanningStage<TEntity> _planningStage;
    private readonly ExecutionStage<TEntity> _executionStage;
    private readonly ResponseMappingStage<TEntity> _mappingStage;
    private readonly IEnumerable<ICrudPipelineHook> _hooks;

    public CrudPipeline(
        MetadataResolutionStage<TEntity> metadataStage,
        ContextEnrichmentStage<TEntity> enrichmentStage,
        ValidationStage<TEntity> validationStage,
        ExecutionPlanningStage<TEntity> planningStage,
        ExecutionStage<TEntity> executionStage,
        ResponseMappingStage<TEntity> mappingStage,
        IEnumerable<ICrudPipelineHook> hooks)
    {
        _metadataStage = metadataStage;
        _enrichmentStage = enrichmentStage;
        _validationStage = validationStage;
        _planningStage = planningStage;
        _executionStage = executionStage;
        _mappingStage = mappingStage;
        _hooks = hooks;
    }

    public async Task<CrudContext<TEntity>> RunAsync(CrudContext<TEntity> context)
    {
        await _metadataStage.ExecuteAsync(context);
        await _enrichmentStage.ExecuteAsync(context);
        await _validationStage.ExecuteAsync(context);

        if (context.ShortCircuited)
        {
            await _mappingStage.ExecuteAsync(context);
            return context;
        }

        foreach (var hook in _hooks) await hook.OnBeforeAsync(context);

        await _planningStage.ExecuteAsync(context);
        await _executionStage.ExecuteAsync(context);

        foreach (var hook in _hooks) await hook.OnAfterAsync(context);

        await _mappingStage.ExecuteAsync(context);
        return context;
    }
}
