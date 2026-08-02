using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline;

public interface IAuthenticationPipeline
{
    Task<AuthenticationContext> RunAsync(AuthenticationContext context);
}
