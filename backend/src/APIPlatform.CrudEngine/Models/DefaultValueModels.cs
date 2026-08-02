namespace APIPlatform.CrudEngine.Models;

/// <summary>Kinds of metadata-driven defaults (Req 8) — deliberately generic engineering
/// concerns only (timestamps/versioning/status), never business defaults.</summary>
public enum DefaultValueKind
{
    UtcNowOnCreate,
    UtcNowOnUpdate,
    ConstantValue,
    IncrementVersion
}

public sealed class DefaultValueBinding
{
    public required string FieldName { get; init; }
    public required DefaultValueKind Kind { get; init; }
    public object? ConstantValue { get; init; }
}

/// <summary>Per-entity set of default-value bindings. SharedSchema/FieldDefinition is frozen, so
/// this config lives in CrudEngine and is supplied by the consuming app via
/// IEntityDefaultValueProvider — never hardcoded in Nucleus.</summary>
public sealed class EntityDefaultValueConfig
{
    public required string EntityName { get; init; }
    public List<DefaultValueBinding> Bindings { get; init; } = new();
}
