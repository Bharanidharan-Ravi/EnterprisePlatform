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
    ///
    /// <para>Two mechanisms, both from the platform package, neither with any schema defined here:
    /// <c>AddDatabaseMigration()</c> is the versioned runner for migrations that ship with a
    /// release (no app currently registers one), and <c>AddSchemaMigration()</c> is the runtime
    /// engine that creates/updates/drops tables from a request body — driven entirely by
    /// SchemaMigrationController, which is the only reason it is registered.</para>
    /// </summary>
    public static IServiceCollection AddAPIPlatformDatabaseMigration(this IServiceCollection services)
    {
        services.AddDatabaseMigration();
        services.AddSchemaMigration();
        return services;
    }
}
