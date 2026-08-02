namespace APIPlatform.Authentication.Models;

/// <summary>Per-app authentication settings resolved in ContextEnrichmentStage from IOptions.
/// Config-driven — apps supply this via appsettings; never hardcoded in the framework.</summary>
public sealed class AuthenticationSettings
{
    public int MaxFailedAttempts { get; init; } = 5;
    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
    public bool RefreshTokenEnabled { get; init; } = true;
    public SessionMode SessionMode { get; init; } = SessionMode.Multi;
    public bool PasswordExpiryEnabled { get; init; }
}
