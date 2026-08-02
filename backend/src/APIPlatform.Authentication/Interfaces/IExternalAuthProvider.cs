using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Extension point for future providers: OAuth, Azure AD, LDAP, OpenID Connect, SAML.
/// Not implemented in V1 — wire new providers here without changing the pipeline or existing
/// stages.</summary>
public interface IExternalAuthProvider
{
    string ProviderId { get; }
    Task<UserInfo?> AuthenticateAsync(string credential, string? tenantId, CancellationToken cancellationToken = default);
}
