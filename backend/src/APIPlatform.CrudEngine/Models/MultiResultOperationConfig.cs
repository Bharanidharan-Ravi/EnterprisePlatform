namespace APIPlatform.CrudEngine.Models;

/// <summary>One result set within a multi-result stored procedure call, mapped to an entity by name.
/// The CLR type is resolved at runtime via <see cref="Interfaces.IEntityTypeRegistry"/> — config only
/// ever carries the entity name string, never a hardcoded Type.</summary>
public sealed class MultiResultBinding
{
    public required string ResultKey { get; init; }
    public required string EntityName { get; init; }
}

/// <summary>
/// Config for a single stored procedure call that returns N result sets in one round trip
/// (e.g. a parent entity plus its child collections). Fully config-driven — adding a new
/// multi-result operation never requires a Nucleus code change, only a new provider entry.
/// </summary>
public sealed class MultiResultOperationConfig
{
    public required string OperationKey { get; init; }
    public required string ProcedureName { get; init; }
    public List<MultiResultBinding> Results { get; init; } = new();
}
