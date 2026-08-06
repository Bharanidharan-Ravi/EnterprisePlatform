using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using APIPlatform.Logging.Abstractions;

namespace APIPlatform.Playground.Services;

public class PlaygroundInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformLogger<PlaygroundInitializationService> _logger;

    public PlaygroundInitializationService(IServiceProvider serviceProvider, IPlatformLogger<PlaygroundInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PlaygroundInitializationService is starting. Initializing database schema if needed.");
        
        using var scope = _serviceProvider.CreateScope();
        var validationService = scope.ServiceProvider.GetRequiredService<PlaygroundValidationService>();

        try
        {
            await validationService.InitializeTableAsync(cancellationToken);
            _logger.LogInformation("Playground validation table successfully verified/created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize playground validation table.");
        }
    }

    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
