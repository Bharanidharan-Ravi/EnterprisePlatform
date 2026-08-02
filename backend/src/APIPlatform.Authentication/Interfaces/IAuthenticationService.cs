using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Primary public API. Applications and controllers depend only on this — the pipeline
/// and all internal stages are implementation details.</summary>
public interface IAuthenticationService
{
    Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResponse> RefreshAsync(string refreshToken, string userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken = default);
}
