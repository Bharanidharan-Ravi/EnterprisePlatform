using APIPlatform.Data.Connections;
using APIPlatform.Data.DependencyInjection;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Providers;
using APIPlatform.Data.StoredProcedures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Database.Tests;

/// <summary>
/// Verifies the AddDatabase()/AddSqlServerProvider()/AddHanaProvider() registration surface —
/// the shape an application actually uses: services.AddSqlServerProvider().AddDatabase(...) or
/// services.AddHanaProvider().AddDatabase(...), never a direct SqlConnection/HanaConnection.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddDatabase_WithSqlServerProvider_ResolvesCoreAbstractions()
    {
        var services = new ServiceCollection();
        services.AddSqlServerProvider();
        services.AddDatabase(options =>
        {
            options.ConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";
            options.Provider = DatabaseProvider.SqlServer;
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IDatabaseProviderFactory>());
        Assert.NotNull(provider.GetService<IDatabaseConnectionFactory>());
        Assert.NotNull(provider.GetService<IDatabaseExecutor>());
        Assert.NotNull(provider.GetService<IStoredProcedureExecutor>());

        var resolved = provider.GetRequiredService<IDatabaseProviderFactory>().GetProvider(DatabaseProvider.SqlServer);
        Assert.Equal(DatabaseProvider.SqlServer, resolved.Kind);
    }

    [Fact]
    public void AddDatabase_WithHanaProvider_ResolvesCoreAbstractions()
    {
        var services = new ServiceCollection();
        services.AddHanaProvider();
        services.AddDatabase(options =>
        {
            options.ConnectionString = "Server=localhost:30015;UserName=SYSTEM;Password=Test123!;";
            options.Provider = DatabaseProvider.Hana;
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IDatabaseProviderFactory>());
        Assert.NotNull(provider.GetService<IDatabaseConnectionFactory>());
        Assert.NotNull(provider.GetService<IDatabaseExecutor>());
        Assert.NotNull(provider.GetService<IStoredProcedureExecutor>());

        var resolved = provider.GetRequiredService<IDatabaseProviderFactory>().GetProvider(DatabaseProvider.Hana);
        Assert.Equal(DatabaseProvider.Hana, resolved.Kind);
    }

    [Fact]
    public void AddDatabase_WithBothProvidersRegistered_FactorySelectsByConfiguredOption()
    {
        // A host that talks to both engines (e.g. during a migration) can register both
        // providers; DatabaseProviderFactory must still hand back the one matching each request,
        // never a hardcoded default.
        var services = new ServiceCollection();
        services.AddSqlServerProvider();
        services.AddHanaProvider();
        services.AddDatabase(options => options.ConnectionString = "unused-for-this-test");

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDatabaseProviderFactory>();

        Assert.IsType<SqlServerDatabaseProvider>(factory.GetProvider(DatabaseProvider.SqlServer));
        Assert.IsType<HanaDatabaseProvider>(factory.GetProvider(DatabaseProvider.Hana));
    }

    [Fact]
    public void AddDatabase_ConnectionFactoryAndExecutor_AreScoped()
    {
        // Guards against a regression back to a singleton/shared connection: these must be
        // resolved per-scope (per-request), never a globally shared connection instance.
        var services = new ServiceCollection();
        services.AddSqlServerProvider();
        services.AddDatabase(options => options.ConnectionString = "unused-for-this-test");

        var connectionFactoryDescriptor = services.Single(d => d.ServiceType == typeof(IDatabaseConnectionFactory));
        var executorDescriptor = services.Single(d => d.ServiceType == typeof(IDatabaseExecutor));

        Assert.Equal(ServiceLifetime.Scoped, connectionFactoryDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, executorDescriptor.Lifetime);
    }
}
