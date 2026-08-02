using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;

namespace APIPlatform.CrudEngine.Engine;

/// <summary>
/// The primary public CRUD API (Req 10). Applications consume this — not IRepository&lt;TEntity&gt;
/// directly — so the pipeline (metadata, defaults, validation, hooks) always runs. Repositories
/// remain available for advanced/manual scenarios but are considered an implementation detail.
/// </summary>
public interface ICrudEngine<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetAsync(EntityKeyValues key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        IReadOnlyDictionary<string, object?>? filters = null,
        IReadOnlyList<SortSpec>? sorting = null,
        PagingSpec? paging = null,
        CancellationToken cancellationToken = default);

    Task<Result<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAsync(EntityKeyValues key, CancellationToken cancellationToken = default);
}
