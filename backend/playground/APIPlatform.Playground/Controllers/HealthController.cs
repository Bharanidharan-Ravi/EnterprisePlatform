using System;
using System.Threading.Tasks;
using APIPlatform.Configuration.Options;
using APIPlatform.Logging.Abstractions;
using APIPlatform.Playground.Models;
using APIPlatform.Validation.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace APIPlatform.Playground.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IPlatformLogger<HealthController> _logger;
    private readonly PlatformOptions _platformOptions;
    private readonly IValidationService _validationService;

    public HealthController(
        IPlatformLogger<HealthController> logger,
        IOptions<PlatformOptions> platformOptions,
        IValidationService validationService)
    {
        _logger = logger;
        _platformOptions = platformOptions.Value;
        _validationService = validationService;
    }

    [HttpGet]
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("Health check requested for {AppName} v{Version}.", _platformOptions.AppName, _platformOptions.Version);

        // Test Validation
        var sample = new SampleRequest { Name = "" };
        var validationResult = await _validationService.ValidateAsync(sample);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        return Ok(new
        {
            Platform = "EnterprisePlatform",
            Product = "APIPlatform",
            Application = "Playground",
            ConfiguredApp = _platformOptions.AppName,
            ConfiguredVersion = _platformOptions.Version,
            ValidationTest = "Passed",
            Status = "Healthy",
            UtcTime = DateTime.UtcNow
        });
    }
}
