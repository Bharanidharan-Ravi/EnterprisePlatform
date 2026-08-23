using APIPlatform.Data.DependencyInjection;
using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.DependencyInjection;
using APIPlatform.Database.Migration.Schema.Abstractions;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Database.Migration.Tests.Fakes;
using APIPlatform.Foundation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.DependencyInjection;

/// <summary>
/// Verifies the AddDatabaseMigration()/AddSchemaMigration() registration surface — mirrors
/// APIPlatform.Database.Tests's DependencyInjectionTests style.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static IServiceCollection BaseServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerProvider();
        services.AddDatabase(options =>
        {
            options.ConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";
            options.Provider = DatabaseProvider.SqlServer;
        });
        services.AddSingleton<IClock>(new FakeClock());
        return services;
    }

    [Fact]
    public void AddDatabaseMigration_ResolvesCoreAbstractions()
    {
        var services = BaseServices();
        services.AddDatabaseMigration();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IMigrationSqlDialectResolver>());
        Assert.NotNull(provider.GetService<IMigrationHistoryRepository>());
        Assert.NotNull(provider.GetService<IMigrationRunner>());
    }

    [Fact]
    public void AddDatabaseMigration_RegistersNoMigrations_UntilAModuleOptsIn()
    {
        var services = BaseServices();
        services.AddDatabaseMigration();

        using var provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IMigration>());
    }

    [Fact]
    public void AddSchemaMigration_ResolvesSchemaMigrationService()
    {
        var services = BaseServices();
        services.AddSchemaMigration();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISchemaMigrationService>());
        Assert.NotNull(provider.GetService<IMigrationSqlDialectResolver>());
    }

    /// <summary>Both extensions register the dialect resolver; calling both must not leave two
    /// competing registrations behind.</summary>
    [Fact]
    public void AddDatabaseMigration_AndAddSchemaMigration_ShareOneDialectResolverRegistration()
    {
        var services = BaseServices();
        services.AddDatabaseMigration();
        services.AddSchemaMigration();

        Assert.Single(services, d => d.ServiceType == typeof(IMigrationSqlDialectResolver));
    }

    [Fact]
    public void AddDatabaseMigration_CoreServices_AreScoped()
    {
        var services = BaseServices();
        services.AddDatabaseMigration();

        var runnerDescriptor = services.Single(d => d.ServiceType == typeof(IMigrationRunner));
        var historyDescriptor = services.Single(d => d.ServiceType == typeof(IMigrationHistoryRepository));

        Assert.Equal(ServiceLifetime.Scoped, runnerDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, historyDescriptor.Lifetime);
    }
}
