using APIPlatform.Data.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public static IServiceCollection AddAPIPlatformDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlServerProvider();
        services.AddDatabase(options => configuration.GetSection("Database").Bind(options));
        return services;
    }
}
