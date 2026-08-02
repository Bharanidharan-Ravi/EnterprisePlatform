using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Results;

namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Generic repository contract constrained to <see cref="IEntity"/>. Uses <see cref="EntityKey"/>
/// rather than a scalar id so single, natural, and composite keys are all supported without
/// breaking this contract later. Version 1 scope is CRUD only — query/search/paging/specifications
/// are intentionally deferred to APIPlatform.Search and APIPlatform.CrudEngine.
/// Reads return plain nullable/list types — "not found" is a normal, cheap outcome that needs
/// no wrapping. Mutations return <see cref="Result{T}"/>/<see cref="OperationResult"/> because
/// Add/Update/Delete can fail for validation or permission reasons that callers should be able
/// to handle without a try/catch at every call site.
/// </summary>
public interface IRepository<TEntity> where TEntity : IEntity
{
    Task<TEntity?> GetByKeyAsync(EntityKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities of this type. Named "List" rather than "GetAll" so the contract doesn't
    /// imply unbounded reads — actual paging/filtering is added at the Search/CrudEngine layer
    /// (Section 3.9), not here, since bounding strategy is query-shape-dependent.
    /// </summary>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(EntityKey key, CancellationToken cancellationToken = default);
}
