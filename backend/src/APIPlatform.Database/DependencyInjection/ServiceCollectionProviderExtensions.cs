using APIPlatform.Data.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Data.DependencyInjection;

/// <summary>
/// Per-provider registration extensions. AddDatabase() itself registers no provider —
/// callers opt into exactly the engine(s) they need, and adding a future provider (Hana,
/// Sqlite, PostgreSql, Oracle) means adding one new AddXxxProvider() method here, with zero
/// changes to AddDatabase() or any existing registration.
/// </summary>
public static class ServiceCollectionProviderExtensions
{
    /// <summary>Registers the SQL Server (and SAP HANA-compatible T-SQL) provider.</summary>
    public static IServiceCollection AddSqlServerProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseProvider, SqlServerDatabaseProvider>();
        return services;
    }
}
