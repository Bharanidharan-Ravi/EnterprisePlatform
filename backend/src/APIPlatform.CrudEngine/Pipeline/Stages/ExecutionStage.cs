using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql;
using APIPlatform.CrudEngine.Sql.Dialects;
using APIPlatform.Data.Execution;
using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 5 — actually executes the plan. Reuses the exact logic previously inline in
/// CrudPipeline: single-row ops go through IRepository&lt;TEntity&gt; (an execution detail);
/// List with a plan goes through QuerySqlBuilder + IDatabaseExecutor directly. No metadata
/// resolution, no validation happens here — only execution.
/// </summary>
public sealed class ExecutionStage<TEntity> : IPipelineStage<TEntity> where TEntity : class, IEntity
{
    private readonly IRepository<TEntity> _repository;
    private readonly IDatabaseExecutor _executor;
    private readonly QuerySqlBuilder _queryBuilder;
    private readonly ISqlDialectResolver _dialectResolver;
    private readonly ITenantContext _tenantContext;

    public ExecutionStage(
        IRepository<TEntity> repository,
        IDatabaseExecutor executor,
        QuerySqlBuilder queryBuilder,
        ISqlDialectResolver dialectResolver,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _executor = executor;
        _queryBuilder = queryBuilder;
        _dialectResolver = dialectResolver;
        _tenantContext = tenantContext;
    }

    public async Task ExecuteAsync(CrudContext<TEntity> context)
    {
        switch (context.Operation)
        {
            case CrudOperationType.GetByKey:
                context.ExecutionResult = await _repository.GetByKeyAsync(ToEntityKey(context.Key), context.CancellationToken);
                break;

            case CrudOperationType.List when context.Plan is { } plan:
                context.ExecutionResult = await ExecutePlannedListAsync(context, plan);
                break;

            case CrudOperationType.List:
                context.ExecutionResult = await _repository.ListAsync(context.CancellationToken);
                break;

            case CrudOperationType.Create:
                context.ExecutionResult = await _repository.AddAsync(context.Entity!, context.CancellationToken);
                break;

            case CrudOperationType.Update:
                context.ExecutionResult = await _repository.UpdateAsync(context.Entity!, context.CancellationToken);
                break;

            case CrudOperationType.Delete:
                context.ExecutionResult = await _repository.DeleteAsync(ToEntityKey(context.Key), context.CancellationToken);
                break;
        }
    }

    private async Task<IReadOnlyList<TEntity>> ExecutePlannedListAsync(CrudContext<TEntity> context, OperationPlan plan)
    {
        var keyFieldNames = context.EntityDefinition!.Fields.Where(f => f.IsPrimaryKey).Select(f => f.Name).ToList();
        var dialect = _dialectResolver.Resolve();
        var (sql, parameters) = _queryBuilder.Build(context.EntityDefinition, plan, keyFieldNames, dialect, _tenantContext.TenantId);
        return await _executor.QueryAsync<TEntity>(sql, parameters, cancellationToken: context.CancellationToken);
    }

    private static EntityKey ToEntityKey(EntityKeyValues? key) => new(key ?? new EntityKeyValues());
}
