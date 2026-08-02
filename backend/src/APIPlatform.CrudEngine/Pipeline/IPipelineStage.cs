using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Pipeline;

/// <summary>
/// One stage in the enterprise execution pipeline. Exactly one responsibility per stage;
/// stages never call each other directly — they only read/write CrudContext. CrudPipeline
/// orchestrates a fixed sequence of these and contains no logic of its own.
/// </summary>
public interface IPipelineStage<TEntity> where TEntity : class
{
    Task ExecuteAsync(CrudContext<TEntity> context);
}
