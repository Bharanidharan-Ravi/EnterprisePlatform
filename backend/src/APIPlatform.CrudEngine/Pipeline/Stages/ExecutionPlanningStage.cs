using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 4 — decides how the operation will execute and produces the immutable OperationPlan.
/// Single-table vs stored-procedure vs SQL-generated routing for GetByKey/Create/Update/Delete is
/// already owned by IRepository&lt;TEntity&gt; (GenericRepository/CompositeRepository) via
/// IEntityOperationBindingProvider config, so this stage's job is building the List query plan
/// from the caller's requested filters/sort/paging. PlanningStage never executes anything.
/// </summary>
public sealed class ExecutionPlanningStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        if (context.Operation != CrudOperationType.List)
            return Task.CompletedTask;

        var hasShape = context.RequestedFilters?.Count > 0 || context.RequestedSorting?.Count > 0 || context.RequestedPaging is not null;
        if (!hasShape)
            return Task.CompletedTask;

        context.Plan = new OperationPlan
        {
            Operation = CrudOperationType.List,
            Filters = context.RequestedFilters ?? new Dictionary<string, object?>(),
            Sorting = context.RequestedSorting ?? Array.Empty<SortSpec>(),
            Paging = context.RequestedPaging
        };

        return Task.CompletedTask;
    }
}
