using APIPlatform.Data.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Data.DependencyInjection;

/// <summary>
/// Per-provider registration extensions. AddDatabase() itself registers no provider —
/// callers opt into exactly the engine(s) they need, and adding a future provider means
/// adding one new AddXxxProvider() method here, with zero changes to AddDatabase() or any
/// existing registration. Register only the provider(s) matching DatabaseOptions.Provider;
/// DatabaseProviderFactory resolves the active one by DatabaseProvider.Kind at runtime.
/// </summary>
public static class ServiceCollectionProviderExtensions
{
    /// <summary>Registers the Microsoft SQL Server provider (Microsoft.Data.SqlClient).</summary>
    public static IServiceCollection AddSqlServerProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseProvider, SqlServerDatabaseProvider>();
        return services;
    }

    /// <summary>Registers the SAP HANA provider (Sap.Data.Hana.Net, the official SAP ADO.NET provider).</summary>
    public static IServiceCollection AddHanaProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseProvider, HanaDatabaseProvider>();
        return services;
    }
}
