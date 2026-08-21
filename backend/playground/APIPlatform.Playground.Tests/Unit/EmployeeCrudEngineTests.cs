using APIPlatform.CrudEngine.Models;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Tests.TestSupport;
using Xunit;

namespace APIPlatform.Playground.Tests.Unit;

/// <summary>
/// UNIT tests (phase2.md 32/33): drive ICrudEngine&lt;Employee&gt; through a fake IDatabaseExecutor.
/// Proves CrudEngine -&gt; SharedSchema metadata resolution -&gt; defaults -&gt; validation -&gt; SQL
/// generation orchestration for Employee. Does NOT prove the SQL executes correctly against a
/// real SQL Server — see Integration/EmployeeSqlServerIntegrationTests for that.
/// </summary>
public sealed class EmployeeCrudEngineTests
{
    private static Employee ValidEmployee(string code = "E001") => new()
    {
        EmployeeCode = code,
        Name = "Ada Lovelace",
        Email = "ada@example.com",
        Department = "Engineering",
        IsActive = true
    };

    [Fact]
    public async Task InsertAsync_ValidEmployee_Succeeds_AndGeneratesInsertSql()
    {
        var host = new EmployeeTestHost();
        var employee = ValidEmployee();

        var result = await host.CrudEngine.InsertAsync(employee);

        Assert.True(result.Succeeded);
        var call = Assert.Single(host.Executor.ExecuteCalls);
        Assert.Contains("INSERT INTO Employees", call.Sql);
        Assert.Contains("EmployeeCode", call.Sql);
    }

    [Fact]
    public async Task InsertAsync_AppliesUtcNowOnCreate_ToCreatedOn()
    {
        var host = new EmployeeTestHost();
        var employee = ValidEmployee();

        await host.CrudEngine.InsertAsync(employee);

        // ContextEnrichmentStage mutates the same entity instance passed in (phase2.md 8:
        // "timestamps handled correctly" via the platform's generic IEntityDefaultValueProvider,
        // never hardcoded in the controller).
        Assert.Equal(host.Clock.UtcNow, employee.CreatedOn);
    }

    [Fact]
    public async Task InsertAsync_MissingName_FailsMetadataValidation()
    {
        var host = new EmployeeTestHost();
        var employee = ValidEmployee();
        employee.Name = "";

        var result = await host.CrudEngine.InsertAsync(employee);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("Name", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(host.Executor.ExecuteCalls); // short-circuited before execution
    }

    [Fact]
    public async Task InsertAsync_MissingEmail_FailsMetadataValidation()
    {
        var host = new EmployeeTestHost();
        var employee = ValidEmployee();
        employee.Email = "";

        var result = await host.CrudEngine.InsertAsync(employee);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAsync_ReturnsMappedEntity_ViaSelectByKey()
    {
        var host = new EmployeeTestHost();
        var id = Guid.NewGuid();
        var expected = ValidEmployee();
        expected.Id = id;
        host.Executor.OnQuerySingleOrDefault = _ => expected;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = id });

        Assert.Same(expected, result);
        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("SELECT * FROM Employees", call.Sql);
        Assert.Contains("Id = @Id", call.Sql);
    }

    [Fact]
    public async Task ListAsync_NoShape_UsesPlainSelectAll()
    {
        var host = new EmployeeTestHost();
        host.Executor.OnQuery = _ => new object[] { ValidEmployee(), ValidEmployee("E002") };

        var result = await host.CrudEngine.ListAsync();

        Assert.Equal(2, result.Count);
        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Equal("SELECT * FROM Employees", call.Sql);
    }

    [Fact]
    public async Task ListAsync_WithEmployeeCodeFilter_GeneratesEqualityWhereClause()
    {
        var host = new EmployeeTestHost();
        host.Executor.OnQuery = _ => new object[] { ValidEmployee() };

        await host.CrudEngine.ListAsync(filters: new Dictionary<string, object?> { ["EmployeeCode"] = "E001" });

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("EmployeeCode = @Filter_EmployeeCode", call.Sql);
        Assert.Equal("E001", call.Parameters!["Filter_EmployeeCode"]);
    }

    [Fact]
    public async Task ListAsync_WithSort_GeneratesOrderByClause()
    {
        var host = new EmployeeTestHost();
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync(sorting: new List<SortSpec> { new("Name", Descending: false) });

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("ORDER BY Name", call.Sql);
    }

    [Fact]
    public async Task ListAsync_WithPaging_GeneratesSqlServerOffsetFetch()
    {
        var host = new EmployeeTestHost();
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync(paging: new PagingSpec(Skip: 10, Take: 5));

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("OFFSET", call.Sql);
        Assert.Contains("FETCH NEXT", call.Sql);
    }

    [Fact]
    public async Task UpdateAsync_ValidEmployee_Succeeds_AndAppliesUtcNowOnUpdate()
    {
        var host = new EmployeeTestHost();
        var employee = ValidEmployee();
        employee.Id = Guid.NewGuid();
        employee.CreatedOn = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await host.CrudEngine.UpdateAsync(employee);

        Assert.True(result.Succeeded);
        Assert.Equal(host.Clock.UtcNow, employee.ModifiedOn);
        // CreatedOn was never touched by CrudEngine (only bound to UtcNowOnCreate, not
        // UtcNowOnUpdate) — the controller is responsible for preserving it by loading the
        // existing row first (see EmployeesController.Update), which this unit test doesn't
        // exercise; here we only assert CrudEngine itself doesn't overwrite it.
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), employee.CreatedOn);
        var call = Assert.Single(host.Executor.ExecuteCalls);
        Assert.Contains("UPDATE Employees SET", call.Sql);
        Assert.DoesNotContain("Id = @Id, ", call.Sql); // primary key is excluded from the SET clause
        Assert.Contains("WHERE Id = @Id", call.Sql);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_AndGeneratesDeleteByKeySql()
    {
        var host = new EmployeeTestHost();
        var id = Guid.NewGuid();

        var result = await host.CrudEngine.DeleteAsync(new EntityKeyValues { ["Id"] = id });

        Assert.True(result.Succeeded);
        var call = Assert.Single(host.Executor.ExecuteCalls);
        Assert.Contains("DELETE FROM Employees", call.Sql);
        Assert.Contains("Id = @Id", call.Sql);
    }
}
