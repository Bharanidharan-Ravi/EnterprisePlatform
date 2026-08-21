using APIPlatform.CrudEngine.DependencyInjection;
using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Engine;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Data.DependencyInjection;
using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.DependencyInjection;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Playground.Defaults;
using APIPlatform.Playground.Metadata;
using APIPlatform.Playground.Migrations;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Playground.Tests.Integration;

/// <summary>
/// INTEGRATION test (phase2.md 33): exercises the real chain
/// CrudEngine&lt;Employee&gt; -&gt; GenericRepository -&gt; Dapper -&gt; SQL Server, against the actual
/// local SQL Server this environment is configured against (same connection string as
/// backend/playground/APIPlatform.Playground/appsettings.Development.json). No fakes anywhere
/// in this class. Ensures the Employees table exists via the platform's real migration engine
/// (IMigrationRunner — idempotent/history-tracked, safe to run every test run), then proves a
/// full create/read/update/delete cycle, cleaning up the row it creates.
/// </summary>
public sealed class EmployeeSqlServerIntegrationTests : IAsyncLifetime
{
    // Mirrors backend/playground/APIPlatform.Playground/appsettings.Development.json's
    // Database:ConnectionString exactly — this is the local SQL Server this dev machine is
    // already configured against (MSSQLSERVER service confirmed running).
    private const string ConnectionString =
        "Data Source=ANARA;Initial Catalog=IQS_DB;User ID=sa;Password=0202;Trust Server Certificate=True";

    private ServiceProvider _services = null!;
    private Guid _createdEmployeeId;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddSqlServerProvider();
        services.AddDatabase(options =>
        {
            options.ConnectionString = ConnectionString;
            options.Provider = DatabaseProvider.SqlServer;
        });
        services.AddDatabaseMigration();
        services.AddScoped<IMigration, EmployeeSqlServerMigration>();

        services.AddSingleton<IClock>(new FakeClock());
        var currentUser = new FakeCurrentUser();
        services.AddSingleton<ICurrentUser>(currentUser);
        services.AddSingleton<ITenantContext>(currentUser);

        services.AddSingleton<IEntityDefinitionProvider, EmployeeEntityDefinitionProvider>();
        services.AddSingleton<IEntityDefaultValueProvider, EmployeeDefaultValueProvider>();
        services.AddCrudEngine();

        _services = services.BuildServiceProvider();

        using var scope = _services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        await runner.RunAsync();
    }

    public async Task DisposeAsync()
    {
        if (_createdEmployeeId != Guid.Empty)
        {
            using var scope = _services.CreateScope();
            var crud = scope.ServiceProvider.GetRequiredService<ICrudEngine<Employee>>();
            await crud.DeleteAsync(new EntityKeyValues { ["Id"] = _createdEmployeeId });
        }
        await _services.DisposeAsync();
    }

    [Fact]
    public async Task FullLifecycle_Create_Get_Update_Delete_AgainstRealSqlServer()
    {
        using var scope = _services.CreateScope();
        var crud = scope.ServiceProvider.GetRequiredService<ICrudEngine<Employee>>();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = $"IT-{Guid.NewGuid():N}"[..12],
            Name = "Integration Test Employee",
            Email = "integration-test@example.com",
            Department = "QA",
            IsActive = true
        };
        _createdEmployeeId = employee.Id;

        // Create
        var insertResult = await crud.InsertAsync(employee);
        Assert.True(insertResult.Succeeded, string.Join(", ", insertResult.Errors.Select(e => e.Message)));

        // Read back — proves the row genuinely exists in SQL Server, not just an in-memory result
        var fetched = await crud.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });
        Assert.NotNull(fetched);
        Assert.Equal(employee.EmployeeCode, fetched!.EmployeeCode);
        Assert.Equal("Integration Test Employee", fetched.Name);
        Assert.True(fetched.CreatedOn > DateTimeOffset.MinValue);

        // Update
        fetched.Department = "Engineering";
        var updateResult = await crud.UpdateAsync(fetched);
        Assert.True(updateResult.Succeeded);

        var afterUpdate = await crud.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });
        Assert.Equal("Engineering", afterUpdate!.Department);
        Assert.Equal(fetched.EmployeeCode, afterUpdate.EmployeeCode); // unrelated fields untouched

        // Delete
        var deleteResult = await crud.DeleteAsync(new EntityKeyValues { ["Id"] = employee.Id });
        Assert.True(deleteResult.Succeeded);

        var afterDelete = await crud.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });
        Assert.Null(afterDelete);

        _createdEmployeeId = Guid.Empty; // already deleted, DisposeAsync doesn't need to clean up
    }
}
