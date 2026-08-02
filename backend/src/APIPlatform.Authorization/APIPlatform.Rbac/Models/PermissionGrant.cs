namespace APIPlatform.Rbac.Models;

/// <summary>
/// Grants a permission (Allow or Deny) to either a role or a user, within one tenant.
/// Exactly one of RoleId / UserId should be set. Deny always overrides Allow when both exist
/// for the same effective permission key (see PermissionResolver).
/// </summary>
public sealed class PermissionGrant
{
    public required string TenantId { get; init; }
    public string? RoleId { get; init; }
    public string? UserId { get; init; }
    public required string PermissionKey { get; init; }
    public required PermissionEffect Effect { get; init; }
}
