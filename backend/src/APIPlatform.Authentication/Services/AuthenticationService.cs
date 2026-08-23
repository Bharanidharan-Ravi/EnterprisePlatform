using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using APIPlatform.Authentication.Pipeline;

namespace APIPlatform.Authentication.Services;

/// <summary>Primary public API. Controllers/consumers depend only on IAuthenticationService.
/// Pipeline and all stages are implementation details.</summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationPipeline _pipeline;
    private readonly IRefreshTokenService    _refresh;
    private readonly ISessionService         _sessions;
    private readonly IIdentityResolver       _identity;
    private readonly IClaimsBuilder          _claims;
    private readonly IJwtService             _jwt;

    public AuthenticationService(
        IAuthenticationPipeline pipeline,
        IRefreshTokenService    refresh,
        ISessionService         sessions,
        IIdentityResolver       identity,
        IClaimsBuilder          claims,
        IJwtService             jwt)
    {
        _pipeline = pipeline;
        _refresh  = refresh;
        _sessions = sessions;
        _identity = identity;
        _claims   = claims;
        _jwt      = jwt;
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(
        AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        var context = new AuthenticationContext
        {
            Request           = request,
            CancellationToken = cancellationToken
        };
        var result = await _pipeline.RunAsync(context);
        return result.Response!;
    }

    public async Task<AuthenticationResponse> RefreshAsync(
        string refreshToken, string userId, CancellationToken cancellationToken = default)
    {
        var valid = await _refresh.ValidateAsync(refreshToken, userId, cancellationToken);
        if (!valid)
            return new AuthenticationResponse { Ok = false, ErrorCode = "INVALID_REFRESH_TOKEN", ErrorMessage = "Refresh token is invalid or expired." };

        // Revoke the session the token being replaced was issued under. This is what actually
        // invalidates any access token still carrying that session's "sid" claim early — JwtBearer
        // checks the session on every request (see Playground's OnTokenValidated). Without this, a
        // JWT is self-contained and stays valid purely on its own signature+expiry until it expires
        // on its own, no matter how many times the caller refreshes.
        var oldSessionId = await _refresh.GetSessionIdAsync(refreshToken, cancellationToken);
        if (oldSessionId is not null)
            await _sessions.RevokeAsync(oldSessionId, cancellationToken);

        // The old token is single-use regardless of outcome from here.
        await _refresh.RevokeAsync(refreshToken, cancellationToken);

        // Full re-auth (password check) is intentionally not re-run — refresh trusts the token,
        // not a password — but the account is re-resolved so a since-deactivated/locked/deleted
        // user cannot silently keep getting new access tokens off an old refresh token.
        var user = await _identity.ResolveByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.IsLocked)
            return new AuthenticationResponse { Ok = false, ErrorCode = "REAUTH_REQUIRED", ErrorMessage = "Please re-authenticate to obtain a new token." };

        var context = new AuthenticationContext
        {
            Request = new AuthenticationRequest { LoginIdentifier = user.Username, Password = string.Empty, TenantId = user.TenantId },
            CancellationToken = cancellationToken,
            User = user,
            CurrentTime = DateTimeOffset.UtcNow
        };

        // A fresh session for the fresh token — its id becomes the new JWT's "sid" claim, and is
        // what a *future* refresh will revoke in turn.
        var newSession = await _sessions.CreateAsync(context, cancellationToken);
        context.SessionId = newSession.SessionId;

        var claims = _claims.Build(context);
        var (accessToken, accessExpiry) = _jwt.Generate(claims);
        var (newRefreshToken, _) = _refresh.Generate(user.UserId, newSession.SessionId);

        return new AuthenticationResponse
        {
            Ok = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = accessExpiry,
            SessionId = newSession.SessionId,
            User = new UserProfile { UserId = user.UserId, Username = user.Username, Email = user.Email, RoleIds = user.RoleIds }
        };
    }

    public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
        => _sessions.RevokeAsync(sessionId, cancellationToken);

    public Task RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken = default)
        => _sessions.RevokeAllForUserAsync(userId, cancellationToken);
}
