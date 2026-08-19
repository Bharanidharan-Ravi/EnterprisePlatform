using APIPlatform.Data.DependencyInjection;
using APIPlatform.Database.Migration.DependencyInjection;
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

    /// <summary>
    /// Wires APIPlatform.Database.Migration against Playground's own configured database — the
    /// "IQS API -> Database.Migration -> IQS Database" shape the platform's migration foundation
    /// is meant to support. Registration only: nothing here runs a migration automatically.
    /// Trigger a run explicitly, e.g. via DatabaseMigrationController's POST /run.
    /// </summary>
    public static IServiceCollection AddAPIPlatformDatabaseMigration(this IServiceCollection services)
    {
        services.AddDatabaseMigration();
        services.AddNotificationSchemaMigrations();
        return services;
    }
}
