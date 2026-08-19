using APIPlatform.Data.DependencyInjection;
using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.DependencyInjection;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Database.Migration.Tests.Fakes;
using APIPlatform.Foundation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.DependencyInjection;

/// <summary>
/// Verifies the AddDatabaseMigration()/AddNotificationSchemaMigrations() registration surface —
/// mirrors APIPlatform.Database.Tests's DependencyInjectionTests style.
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
    public void AddNotificationSchemaMigrations_RegistersBothProviderVariants()
    {
        var services = BaseServices();
        services.AddDatabaseMigration();
        services.AddNotificationSchemaMigrations();

        using var provider = services.BuildServiceProvider();
        var migrations = provider.GetServices<IMigration>().ToList();

        Assert.Equal(2, migrations.Count);
        Assert.Contains(migrations, m => m.SupportedProvider == DatabaseProvider.SqlServer);
        Assert.Contains(migrations, m => m.SupportedProvider == DatabaseProvider.Hana);
        Assert.All(migrations, m => Assert.Equal("Notification.Schema.v1", m.MigrationId));
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
