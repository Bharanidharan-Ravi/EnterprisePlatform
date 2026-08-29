using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 4 — decides how the operation will execute and produces the immutable OperationPlan.
/// Single-table vs stored-procedure vs SQL-generated routing for GetByKey/Create/Update/Delete is
/// already owned by IRepository&lt;TEntity&gt; (GenericRepository/CompositeRepository) via
/// IEntityOperationBindingProvider config, so this stage's job is building the List query plan
/// from the caller's requested filters/sort/paging, plus any filter a hook imposed
/// (CrudContext.AdditionalFilters). PlanningStage never executes anything.
/// </summary>
public sealed class ExecutionPlanningStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        if (context.Operation != CrudOperationType.List)
            return Task.CompletedTask;

        var hasShape = context.RequestedFilters?.Count > 0
            || context.AdditionalFilters.Count > 0
            || context.RequestedSorting?.Count > 0
            || context.RequestedPaging is not null;
        if (!hasShape)
            return Task.CompletedTask;

        context.Plan = new OperationPlan
        {
            Operation = CrudOperationType.List,
            Filters = MergeFilters(context),
            Sorting = context.RequestedSorting ?? Array.Empty<SortSpec>(),
            Paging = context.RequestedPaging
        };

        return Task.CompletedTask;
    }

    /// <summary>
    /// Caller-requested filters first, then hook-imposed ones — so an imposed filter overwrites a
    /// caller's value for the same field rather than the other way round. That ordering is the
    /// security-relevant half: a row-scoping hook's predicate must survive a caller naming the
    /// same field in its own query string.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> MergeFilters(CrudContext<TEntity> context)
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (context.RequestedFilters is not null)
            foreach (var (field, value) in context.RequestedFilters) merged[field] = value;

        foreach (var (field, value) in context.AdditionalFilters) merged[field] = value;

        return merged;
    }
}
