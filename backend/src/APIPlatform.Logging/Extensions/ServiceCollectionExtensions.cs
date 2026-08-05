using APIPlatform.Logging.Abstractions;
using APIPlatform.Logging.Options;
using APIPlatform.Logging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Logging.Extensions;

/// <summary>
/// Provides extension methods for registering logging services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APIPlatform Logging module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAPIPlatformLogging(this IServiceCollection services)
    {
        services.AddOptions<LoggingOptions>()
            .BindConfiguration(LoggingOptions.SectionName);

        // Register the generic logger abstraction
        services.AddTransient(typeof(IPlatformLogger<>), typeof(PlatformLogger<>));

        return services;
    }
}
