using System.Security.Cryptography;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using Microsoft.Extensions.Options;
using APIPlatform.Authentication.Jwt;

namespace APIPlatform.Authentication.Session;

public sealed class SessionService : ISessionService
{
    private readonly ISessionStore _store;
    private readonly JwtOptions _jwtOptions;

    public SessionService(ISessionStore store, IOptions<JwtOptions> jwtOptions)
    {
        _store = store;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<SessionInfo> CreateAsync(AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        // Single-session mode: revoke all existing sessions before creating
        if (context.Settings?.SessionMode == SessionMode.Single && context.User is not null)
            await _store.DeleteAllForUserAsync(context.User.UserId, cancellationToken);

        var session = new SessionInfo
        {
            SessionId = GenerateId(),
            UserId = context.User!.UserId,
            CreatedAt = context.CurrentTime,
            ExpiresAt = context.CurrentTime.AddMinutes(_jwtOptions.ExpiryMinutes),
            DeviceId = context.Device?.DeviceId,
            ClientIp = context.Device?.ClientIp
        };
        await _store.SaveAsync(session, cancellationToken);
        return session;
    }

    public Task<SessionInfo?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
        => _store.FindAsync(sessionId, cancellationToken);

    public async Task<bool> ValidateAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _store.FindAsync(sessionId, cancellationToken);
        return session is not null && session.ExpiresAt > DateTimeOffset.UtcNow;
    }

    public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
        => _store.DeleteAsync(sessionId, cancellationToken);

    public Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        => _store.DeleteAllForUserAsync(userId, cancellationToken);

    private static string GenerateId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
               .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
