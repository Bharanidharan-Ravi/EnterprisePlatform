using Nucleus.SharedSchema.Enums;
namespace Nucleus.SharedSchema.Models;
public sealed record FieldDefinition
{
    public required string Name { get; init; }
    public required FieldDataType DataType { get; init; }
    public bool IsNullable { get; init; }
    public required FieldSourceType SourceType { get; init; }
    public string? SourceFieldName { get; init; }
    public bool IsPrimaryKey { get; init; }
    public IReadOnlyList<string>? EnumValues { get; init; }
    public string? DefaultValue { get; init; }
    public string? SourcedViaRelationshipName { get; init; }
    public ValidationRuleDefinition? Validation { get; init; }
    public UiHintDefinition? UiHint { get; init; }
    public PermissionRequirement? Permissions { get; init; }
}
