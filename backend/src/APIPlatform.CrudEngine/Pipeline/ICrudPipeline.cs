using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Pipeline;

/// <summary>
/// Runs one CrudContext through the full lifecycle (Req 2):
/// Metadata Resolution → Default Values → Validation → Before Hooks → Operation Planning →
/// SQL/Procedure Execution → After Hooks → Result Mapping.
/// Repositories (GenericRepository/CompositeRepository) are invoked only as the execution detail
/// inside this pipeline — they are no longer the primary abstraction (Req 1).
/// </summary>
public interface ICrudPipeline<TEntity> where TEntity : class
{
    Task<CrudContext<TEntity>> RunAsync(CrudContext<TEntity> context);
}
