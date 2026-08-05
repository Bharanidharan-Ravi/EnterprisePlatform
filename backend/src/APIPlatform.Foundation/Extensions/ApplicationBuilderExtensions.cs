using Microsoft.AspNetCore.Builder;

namespace APIPlatform.Foundation.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IApplicationBuilder"/> to configure the Foundation module pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the APIPlatform Foundation module in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseAPIPlatformFoundation(this IApplicationBuilder app)
    {
        return app;
    }
}
