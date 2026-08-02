namespace APIPlatform.Authentication.Models;

/// <summary>Public-facing response built by ResponseMappingStage. Deliberately hides internal
/// models — callers only see what they need.</summary>
public sealed class AuthenticationResponse
{
    public required bool Ok { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? SessionId { get; init; }
    public UserProfile? User { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class UserProfile
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public IReadOnlyList<string> RoleIds { get; init; } = Array.Empty<string>();
}
