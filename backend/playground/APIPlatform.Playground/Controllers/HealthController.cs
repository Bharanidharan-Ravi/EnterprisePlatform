using System;
using APIPlatform.Configuration.Options;
using APIPlatform.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace APIPlatform.Playground.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IPlatformLogger<HealthController> _logger;
    private readonly PlatformOptions _platformOptions;

    public HealthController(
        IPlatformLogger<HealthController> logger,
        IOptions<PlatformOptions> platformOptions)
    {
        _logger = logger;
        _platformOptions = platformOptions.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check requested for {AppName} v{Version}.", _platformOptions.AppName, _platformOptions.Version);

        return Ok(new
        {
            Platform = "EnterprisePlatform",
            Product = "APIPlatform",
            Application = "Playground",
            ConfiguredApp = _platformOptions.AppName,
            ConfiguredVersion = _platformOptions.Version,
            Status = "Healthy",
            UtcTime = DateTime.UtcNow
        });
    }
}
