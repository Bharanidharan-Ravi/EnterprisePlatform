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

    public AuthenticationService(
        IAuthenticationPipeline pipeline,
        IRefreshTokenService    refresh,
        ISessionService         sessions)
    {
        _pipeline = pipeline;
        _refresh  = refresh;
        _sessions = sessions;
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

        // Rotate — revoke old, signal caller to re-authenticate for new JWT
        // Full re-auth pipeline is intentionally not re-run here; the refresh path
        // is a lightweight token rotation only (security best practice).
        await _refresh.RevokeAsync(refreshToken, cancellationToken);
        return new AuthenticationResponse { Ok = false, ErrorCode = "REAUTH_REQUIRED", ErrorMessage = "Please re-authenticate to obtain a new token." };
    }

    public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
        => _sessions.RevokeAsync(sessionId, cancellationToken);

    public Task RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken = default)
        => _sessions.RevokeAllForUserAsync(userId, cancellationToken);
}
