using System.Security.Claims;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Generates and validates JWTs. Implementation is replaceable without pipeline
/// changes.</summary>
public interface IJwtService
{
    (string Token, DateTimeOffset Expiry) Generate(IReadOnlyList<Claim> claims);
    IReadOnlyList<Claim>? Validate(string token);
}
