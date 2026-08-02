namespace APIPlatform.Rbac.Models;

/// <summary>
/// A role, per Master Plan Section 3.4 (Models/Role). ParentRoleId supports role hierarchy
/// (child roles inherit parent grants) — resolved by IRoleStore.GetEffectiveRolesForUserAsync.
/// </summary>
public sealed class Role
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string TenantId { get; init; }
    public string? ParentRoleId { get; init; }
    public bool IsSystemRole { get; init; }
}
