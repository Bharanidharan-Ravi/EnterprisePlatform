using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Pipeline;
using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;

namespace APIPlatform.CrudEngine.Engine;

/// <summary>Default ICrudEngine&lt;TEntity&gt; — builds a CrudContext per call and runs it
/// through ICrudPipeline&lt;TEntity&gt;. This class, not GenericRepository/CompositeRepository, is
/// the intended consuming-app entry point (Req 1, Req 10).</summary>
public sealed class CrudEngine<TEntity> : ICrudEngine<TEntity> where TEntity : class, IEntity
{
    private readonly ICrudPipeline<TEntity> _pipeline;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly string _entityName = typeof(TEntity).Name;

    public CrudEngine(ICrudPipeline<TEntity> pipeline, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _pipeline = pipeline;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<TEntity?> GetAsync(EntityKeyValues key, CancellationToken cancellationToken = default)
    {
        var context = NewContext(CrudOperationType.GetByKey, cancellationToken);
        context.Key = key;
        var result = await _pipeline.RunAsync(context);
        return result.ExecutionResult as TEntity;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        IReadOnlyDictionary<string, object?>? filters = null,
        IReadOnlyList<SortSpec>? sorting = null,
        PagingSpec? paging = null,
        CancellationToken cancellationToken = default)
    {
        var context = NewContext(CrudOperationType.List, cancellationToken, filters, sorting, paging);
        var result = await _pipeline.RunAsync(context);
        return result.ExecutionResult as IReadOnlyList<TEntity> ?? Array.Empty<TEntity>();
    }

    public async Task<Result<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var context = NewContext(CrudOperationType.Create, cancellationToken);
        context.Entity = entity;
        var result = await _pipeline.RunAsync(context);

        if (result.ShortCircuited)
            return Result<TEntity>.Failure(result.Error ?? new ErrorInfo { Code = "validation_failed", Message = "Validation failed." });

        return (Result<TEntity>)result.ExecutionResult!;
    }

    public async Task<OperationResult> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var context = NewContext(CrudOperationType.Update, cancellationToken);
        context.Entity = entity;
        var result = await _pipeline.RunAsync(context);

        if (result.ShortCircuited)
            return OperationResult.Failure(result.Error ?? new ErrorInfo { Code = "validation_failed", Message = "Validation failed." });

        return (OperationResult)result.ExecutionResult!;
    }

    public async Task<OperationResult> DeleteAsync(EntityKeyValues key, CancellationToken cancellationToken = default)
    {
        var context = NewContext(CrudOperationType.Delete, cancellationToken);
        context.Key = key;
        var result = await _pipeline.RunAsync(context);
        return (OperationResult)result.ExecutionResult!;
    }

    private CrudContext<TEntity> NewContext(
        CrudOperationType operation,
        CancellationToken ct,
        IReadOnlyDictionary<string, object?>? requestedFilters = null,
        IReadOnlyList<SortSpec>? requestedSorting = null,
        PagingSpec? requestedPaging = null) => new()
    {
        Operation = operation,
        EntityName = _entityName,
        CurrentUser = _currentUser,
        TenantContext = _tenantContext,
        CancellationToken = ct,
        RequestedFilters = requestedFilters,
        RequestedSorting = requestedSorting,
        RequestedPaging = requestedPaging
    };
}
