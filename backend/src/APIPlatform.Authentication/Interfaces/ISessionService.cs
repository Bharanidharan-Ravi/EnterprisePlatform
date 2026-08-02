using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

public sealed class SessionInfo
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public string? DeviceId { get; init; }
    public string? ClientIp { get; init; }
}

public interface ISessionService
{
    Task<SessionInfo> CreateAsync(AuthenticationContext context, CancellationToken cancellationToken = default);
    Task<SessionInfo?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
