namespace APIPlatform.CrudEngine.Models;

/// <summary>Framework response model built by ResponseMappingStage (Mapping Stage). Additive —
/// ICrudEngine currently reads CrudContext.ExecutionResult directly for backward compatibility;
/// Response carries the same outcome in a stage-produced, uniform shape for future consumers
/// (e.g. controllers) that want it without knowing the entity's raw execution result type.</summary>
public sealed class CrudResponse<TEntity> where TEntity : class
{
    public required bool Ok { get; init; }
    public object? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
