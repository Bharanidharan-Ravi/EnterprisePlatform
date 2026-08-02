namespace APIPlatform.Authentication.Models;

public sealed class AuthenticationResult
{
    public required bool Succeeded { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset? AccessTokenExpiry { get; init; }
    public DateTimeOffset? RefreshTokenExpiry { get; init; }
    public string? SessionId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public UserInfo? User { get; init; }

    public static AuthenticationResult Success(string accessToken, string? refreshToken,
        DateTimeOffset accessTokenExpiry, DateTimeOffset? refreshTokenExpiry,
        string? sessionId, UserInfo user) =>
        new()
        {
            Succeeded = true, AccessToken = accessToken, RefreshToken = refreshToken,
            AccessTokenExpiry = accessTokenExpiry, RefreshTokenExpiry = refreshTokenExpiry,
            SessionId = sessionId, User = user
        };

    public static AuthenticationResult Failure(string code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}
