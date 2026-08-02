using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Unified entity service — coordinates access to an entity via IRepository&lt;TEntity&gt;
/// and exposes both read and write operations. Consumed by generated apps or the future
/// APIPlatform.Builder for generic screen CRUD.
/// </summary>
public interface IEntityService<TEntity> where TEntity : class, IEntity
{
    IRepository<TEntity> Repository { get; }
}
