namespace APIPlatform.Authentication.Events;

public enum AuthenticationEventType
{
    LoginSuccess, LoginFailed, Logout, AccountLocked,
    PasswordChanged, PasswordReset, RefreshTokenGenerated, SessionRevoked
}

/// <summary>Published after execution; Audit module subscribes later without changing this
/// package.</summary>
public sealed class AuthenticationEvent
{
    public required AuthenticationEventType EventType { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? TenantId { get; init; }
    public string? ClientIp { get; init; }
    public string? DeviceId { get; init; }
    public string? ErrorCode { get; init; }
    public IDictionary<string, string> Extra { get; init; } = new Dictionary<string, string>();
}
