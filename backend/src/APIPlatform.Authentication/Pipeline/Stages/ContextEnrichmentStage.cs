using APIPlatform.Authentication.Models;
using Microsoft.Extensions.Options;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 2 — enriches context with all runtime data needed before validation.
/// CurrentTime, Device, CorrelationId, Settings. Nothing is validated or executed here.</summary>
public sealed class ContextEnrichmentStage : IAuthenticationStage
{
    private readonly AuthenticationSettings _settings;

    public ContextEnrichmentStage(IOptions<AuthenticationSettings> settings)
        => _settings = settings.Value;

    public Task ExecuteAsync(AuthenticationContext context)
    {
        context.Settings    = _settings;
        context.CurrentTime = DateTimeOffset.UtcNow;
        context.CorrelationId = Guid.NewGuid().ToString();
        context.RequestId   = Guid.NewGuid().ToString();
        context.Device = new DeviceInfo
        {
            DeviceId  = context.Request.DeviceId,
            ClientIp  = context.Request.ClientIp,
            UserAgent = context.Request.UserAgent
        };
        return Task.CompletedTask;
    }
}
