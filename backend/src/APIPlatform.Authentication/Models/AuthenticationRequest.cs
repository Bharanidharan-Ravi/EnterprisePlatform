namespace APIPlatform.Authentication.Models;

public sealed class AuthenticationRequest
{
    public required string LoginIdentifier { get; init; }
    public required string Password { get; init; }
    public string? TenantId { get; init; }
    public string? ApplicationId { get; init; }
    public string? DeviceId { get; init; }
    public string? ClientIp { get; init; }
    public string? UserAgent { get; init; }
    public IReadOnlyDictionary<string, string> Extra { get; init; } = new Dictionary<string, string>();
}
