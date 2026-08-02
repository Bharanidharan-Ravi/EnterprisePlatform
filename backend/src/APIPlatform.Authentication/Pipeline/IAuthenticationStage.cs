using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline;

/// <summary>Single-responsibility stage in the authentication pipeline. Stages communicate
/// only through AuthenticationContext — never with each other directly.</summary>
public interface IAuthenticationStage
{
    Task ExecuteAsync(AuthenticationContext context);
}
