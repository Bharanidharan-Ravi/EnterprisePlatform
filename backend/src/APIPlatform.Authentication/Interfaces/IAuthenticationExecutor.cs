using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Executes authentication: verifies credentials, generates tokens, creates session.
/// Extension point for future providers (OAuth, LDAP) — register an alternate implementation
/// per AuthenticationStrategyType via a provider registry without changing the pipeline.</summary>
public interface IAuthenticationExecutor
{
    Task ExecuteAsync(AuthenticationContext context);
}
