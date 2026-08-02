using APIPlatform.Foundation.Entities;
using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.CrudEngine.Services;

/// <summary>Default IEntityService&lt;TEntity&gt; — exposes the repository directly.</summary>
public sealed class EntityService<TEntity> : IEntityService<TEntity> where TEntity : class, IEntity
{
    public EntityService(IRepository<TEntity> repository) => Repository = repository;
    public IRepository<TEntity> Repository { get; }
}
