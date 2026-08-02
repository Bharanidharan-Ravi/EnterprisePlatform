using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 5 — executes the plan: verifies credentials, generates claims + tokens,
/// creates session. No metadata resolution, no validation, no planning.</summary>
public sealed class AuthenticationExecutionStage : IAuthenticationStage
{
    private readonly IAuthenticationExecutor _executor;

    public AuthenticationExecutionStage(IAuthenticationExecutor executor) => _executor = executor;

    public Task ExecuteAsync(AuthenticationContext context) => _executor.ExecuteAsync(context);
}
