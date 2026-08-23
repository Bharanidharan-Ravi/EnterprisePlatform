namespace APIPlatform.Authentication.Interfaces;

public interface IRefreshTokenService
{
    /// <summary><paramref name="sessionId"/> is stored alongside the token so a later refresh can
    /// find and revoke the session the token being replaced was issued under — see
    /// <see cref="GetSessionIdAsync"/>.</summary>
    (string Token, DateTimeOffset Expiry) Generate(string userId, string? sessionId = null);
    Task<bool> ValidateAsync(string token, string userId, CancellationToken cancellationToken = default);

    /// <summary>The session id <paramref name="token"/> was generated with, or null if none/not
    /// found. Used by refresh to revoke the old session — the mechanism that actually invalidates
    /// an old access token early, since the JWT itself stays valid on its own until it expires.</summary>
    Task<string?> GetSessionIdAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
