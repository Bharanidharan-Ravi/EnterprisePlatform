namespace APIPlatform.Rbac.Models;

/// <summary>
/// Field-level access rule. Absence of a rule for a given field means "no additional
/// restriction beyond the entity-level Crud permission" — field rules only ever narrow access
/// further, they never grant access the entity-level check didn't already allow.
/// </summary>
public sealed class FieldPermissionRule
{
    public required string EntityKey { get; init; }
    public required string FieldKey { get; init; }
    public required string PermissionKey { get; init; }
    public required FieldAccess Access { get; init; }
}
