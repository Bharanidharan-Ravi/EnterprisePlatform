using APIPlatform.Foundation.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Foundation.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register the Foundation module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APIPlatform Foundation module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAPIPlatformFoundation(this IServiceCollection services)
    {
        services.AddOptions<FoundationOptions>()
            .BindConfiguration(FoundationOptions.SectionName);

        return services;
    }
}
