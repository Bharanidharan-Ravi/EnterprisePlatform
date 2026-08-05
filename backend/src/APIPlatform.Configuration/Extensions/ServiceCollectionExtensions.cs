using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Configuration.Extensions;

/// <summary>
/// Provides extension methods for registering the configuration module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APIPlatform Configuration module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The root configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAPIPlatformConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Configuration is already registered by default in ASP.NET Core, 
        // but we can register it explicitly if needed by other non-web hosts.
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// A helper method to easily bind strongly typed options to a configuration section and register it for dependency injection.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The section name in the configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection BindPlatformOptions<TOptions>(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string sectionName) where TOptions : class, new()
    {
        services.AddOptions<TOptions>()
                .Bind(configuration.GetSection(sectionName))
                .ValidateDataAnnotations();

        return services;
    }
}
