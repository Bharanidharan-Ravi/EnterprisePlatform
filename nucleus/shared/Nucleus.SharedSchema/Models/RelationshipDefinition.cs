namespace Nucleus.SharedSchema.Models;
public sealed record RelationshipDefinition
{
    public required string TargetEntityName { get; init; }
    public required string LocalKeyField { get; init; }
    public required string TargetKeyField { get; init; }
    public int? JoinChainOrder { get; init; }
}
