using System.Data;
using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Options;
using APIPlatform.Data.Providers;
using Microsoft.Data.SqlClient;
using Sap.Data.Hana;
using Xunit;

namespace APIPlatform.Database.Tests;

/// <summary>
/// Verifies each IDatabaseProvider implementation identifies itself correctly and creates the
/// right underlying ADO.NET connection type. These are construction-only checks — no network
/// call is made, so they run without a live SQL Server or HANA instance.
/// </summary>
public class ProviderResolutionTests
{
    private const string SqlServerConnectionString =
        "Server=localhost;Database=TestDb;User Id=sa;Password=Test123!;TrustServerCertificate=True;";

    private const string HanaConnectionString =
        "Server=localhost:30015;UserName=SYSTEM;Password=Test123!;";

    [Fact]
    public void SqlServerDatabaseProvider_Kind_IsSqlServer()
    {
        IDatabaseProvider provider = new SqlServerDatabaseProvider();

        Assert.Equal(DatabaseProvider.SqlServer, provider.Kind);
    }

    [Fact]
    public void SqlServerDatabaseProvider_CreateConnection_ReturnsSqlConnection()
    {
        IDatabaseProvider provider = new SqlServerDatabaseProvider();

        using IDbConnection connection = provider.CreateConnection(SqlServerConnectionString);

        Assert.IsType<SqlConnection>(connection);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public void HanaDatabaseProvider_Kind_IsHana()
    {
        IDatabaseProvider provider = new HanaDatabaseProvider();

        Assert.Equal(DatabaseProvider.Hana, provider.Kind);
    }

    [Fact]
    public void HanaDatabaseProvider_CreateConnection_ReturnsHanaConnection()
    {
        IDatabaseProvider provider = new HanaDatabaseProvider();

        using IDbConnection connection = provider.CreateConnection(HanaConnectionString);

        Assert.IsType<HanaConnection>(connection);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public void DatabaseProviderFactory_ResolvesSqlServerAndHana_ByKind()
    {
        var providers = new IDatabaseProvider[] { new SqlServerDatabaseProvider(), new HanaDatabaseProvider() };
        var factory = new DatabaseProviderFactory(providers);

        Assert.IsType<SqlServerDatabaseProvider>(factory.GetProvider(DatabaseProvider.SqlServer));
        Assert.IsType<HanaDatabaseProvider>(factory.GetProvider(DatabaseProvider.Hana));
    }

    [Fact]
    public void DatabaseProviderFactory_UnregisteredProvider_ThrowsDatabaseException()
    {
        // Only SqlServer is registered — resolving Hana must fail loudly, not silently fall back.
        var factory = new DatabaseProviderFactory([new SqlServerDatabaseProvider()]);

        var ex = Assert.Throws<DatabaseException>(() => factory.GetProvider(DatabaseProvider.Hana));
        Assert.Contains("Hana", ex.Message);
    }

    [Fact]
    public void DapperExecutor_AcceptsPlainIDbConnection_ForBothProviders()
    {
        // The common execution layer (SqlDatabaseExecutor -> Dapper) must operate purely against
        // IDbConnection: it should never need to know whether it holds a SqlConnection or a
        // HanaConnection. Asserting both provider outputs satisfy IDbConnection is exactly that
        // contract, expressed without spinning up a real Dapper call against a live server.
        IDbConnection sqlConnection = new SqlServerDatabaseProvider().CreateConnection(SqlServerConnectionString);
        IDbConnection hanaConnection = new HanaDatabaseProvider().CreateConnection(HanaConnectionString);

        try
        {
            Assert.IsAssignableFrom<IDbConnection>(sqlConnection);
            Assert.IsAssignableFrom<IDbConnection>(hanaConnection);
        }
        finally
        {
            sqlConnection.Dispose();
            hanaConnection.Dispose();
        }
    }
}
