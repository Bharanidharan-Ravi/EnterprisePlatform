using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Defaults;

/// <summary>Supplies EntityDefaultValueConfig per entity — app-owned config, mirrors
/// IEntityOperationBindingProvider's pattern.</summary>
public interface IEntityDefaultValueProvider
{
    EntityDefaultValueConfig? TryGetConfig(string entityName);
}

/// <summary>Applies configured defaults to CrudContext.Entity before validation runs.</summary>
public interface IDefaultValueProcessor
{
    void Apply<TEntity>(CrudContext<TEntity> context) where TEntity : class;
}
