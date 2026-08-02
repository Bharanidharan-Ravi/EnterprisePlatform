namespace APIPlatform.Authentication.Jwt;

/// <summary>Bound from appsettings (AddAuthentication() registers IOptions&lt;JwtOptions&gt;).
/// Never hardcoded in the framework.</summary>
public sealed class JwtOptions
{
    public const string Section = "Authentication:Jwt";
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 30;
}
