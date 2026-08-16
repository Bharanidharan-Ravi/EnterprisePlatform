using APIPlatform.Data.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace APIPlatform.Database.Tests;

/// <summary>
/// Verifies the appsettings.json shape described in the platform docs — a "Database" section
/// with "Provider" and "ConnectionString" — binds to DatabaseOptions correctly for both engines.
/// APIPlatform.Data never reads configuration itself; this exercises exactly what a consuming
/// application does via `configuration.GetSection("Database").Bind(options)`.
/// </summary>
public class ConfigurationBindingTests
{
    [Fact]
    public void Configuration_WithSqlServerProvider_BindsToSqlServer()
    {
        var options = BindFrom(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;Trusted_Connection=True;"
        });

        Assert.Equal(DatabaseProvider.SqlServer, options.Provider);
        Assert.Equal("Server=localhost;Database=TestDb;Trusted_Connection=True;", options.ConnectionString);
    }

    [Fact]
    public void Configuration_WithHanaProvider_BindsToHana()
    {
        var options = BindFrom(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Hana",
            ["Database:ConnectionString"] = "Server=localhost:30015;UserName=SYSTEM;Password=Test123!;"
        });

        Assert.Equal(DatabaseProvider.Hana, options.Provider);
        Assert.Equal("Server=localhost:30015;UserName=SYSTEM;Password=Test123!;", options.ConnectionString);
    }

    [Fact]
    public void Configuration_OmittingProvider_DefaultsToSqlServer()
    {
        var options = BindFrom(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;Trusted_Connection=True;"
        });

        Assert.Equal(DatabaseProvider.SqlServer, options.Provider);
    }

    private static DatabaseOptions BindFrom(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var options = new DatabaseOptions { ConnectionString = string.Empty };
        configuration.GetSection("Database").Bind(options);
        return options;
    }
}
