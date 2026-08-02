using APIPlatform.Data.Connections;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Providers;
using APIPlatform.Data.Resilience;
using APIPlatform.Data.StoredProcedures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APIPlatform.Data.DependencyInjection;

/// <summary>
/// Registers APIPlatform.Data services. Follows the platform-wide AddXxx() DI convention.
/// Registers no IDatabaseProvider itself — call the matching AddXxxProvider() (e.g.
/// AddSqlServerProvider()) beforehand so provider support stays additive and extensible.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, Action<DatabaseOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton<IDatabaseProviderFactory, DatabaseProviderFactory>();
        services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
        services.AddScoped<IDatabaseExecutor, SqlDatabaseExecutor>();
        services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

        // Replaceable default — a real resilience policy can override this registration later
        // without any change to SqlDatabaseExecutor.
        services.TryAddSingleton<IDatabaseRetryPolicy, NoOpDatabaseRetryPolicy>();

        return services;
    }
}
