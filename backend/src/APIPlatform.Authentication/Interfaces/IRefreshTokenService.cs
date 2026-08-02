namespace APIPlatform.Authentication.Interfaces;

public interface IRefreshTokenService
{
    (string Token, DateTimeOffset Expiry) Generate(string userId);
    Task<bool> ValidateAsync(string token, string userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
