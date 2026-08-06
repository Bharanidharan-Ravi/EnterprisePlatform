using APIPlatform.Validation.Abstractions;
using APIPlatform.Validation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Validation.Extensions;

/// <summary>
/// Provides extension methods for registering the validation module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APIPlatform Validation module to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAPIPlatformValidation(this IServiceCollection services)
    {
        services.AddTransient<IValidationService, ValidationService>();

        return services;
    }
}
