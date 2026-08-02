using Nucleus.SharedSchema.Enums;
namespace Nucleus.SharedSchema.Models;
public sealed record EntityDefinition
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required FieldSourceType SourceType { get; init; }
    public required string SourceName { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public bool IsTenantScoped { get; init; }
    public required IReadOnlyList<FieldDefinition> Fields { get; init; }
    public IReadOnlyList<RelationshipDefinition> Relationships { get; init; } = Array.Empty<RelationshipDefinition>();
}
