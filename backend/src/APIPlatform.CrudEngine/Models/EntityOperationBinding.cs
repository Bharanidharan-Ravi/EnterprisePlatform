namespace APIPlatform.CrudEngine.Models;

/// <summary>
/// Declares that one or more <see cref="CrudOperationType"/> operations for an entity should
/// execute via a stored procedure instead of generated SQL. Supplied entirely by config through
/// <see cref="Interfaces.IEntityOperationBindingProvider"/> — never hardcoded in Nucleus.
/// Operations with no entry here fall back to SqlQueryBuilder-generated SQL.
/// </summary>
public sealed class EntityOperationBinding
{
    /// <summary>Entity name — must match EntityDefinition.Name (SharedSchema).</summary>
    public required string EntityName { get; init; }

    /// <summary>Stored procedure name per operation. Missing key = use generated SQL for that op.</summary>
    public Dictionary<CrudOperationType, string> ProcedureNames { get; init; } = new();

    /// <summary>True if List should pass through the multi-result path (see <see cref="MultiResultOperationConfig"/>)
    /// instead of a single-result procedure/query.</summary>
    public bool ListIsMultiResult { get; init; }

    /// <summary>When ListIsMultiResult is true, the operation key to resolve via IMultiResultOperationProvider.</summary>
    public string? MultiResultOperationKey { get; init; }
}
