namespace APIPlatform.Authentication.Models;

public sealed class UserInfo
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public string? EmployeeCode { get; init; }
    public required string PasswordHash { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsLocked { get; init; }
    public int FailedAttemptCount { get; init; }
    public DateTimeOffset? LockedUntil { get; init; }
    public DateTimeOffset? PasswordExpiresAt { get; init; }
    public string? TenantId { get; init; }
    public string? CompanyId { get; init; }
    public string? BranchId { get; init; }
    public string? DepartmentId { get; init; }
    public IReadOnlyList<string> RoleIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PermissionIds { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> ExtendedClaims { get; init; } = new Dictionary<string, string>();
}
