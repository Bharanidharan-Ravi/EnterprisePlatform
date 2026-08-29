using APIPlatform.CrudEngine.Models;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Rbac;
using APIPlatform.Playground.Tests.TestSupport;
using APIPlatform.Rbac.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Playground.Tests.Unit;

/// <summary>
/// UNIT tests for Phase 2 (row/data-level scoping): a role sees only the rows it is entitled to.
/// Drives the real ICrudEngine&lt;Employee&gt; through the real RowScopeCrudHook and the real Rbac
/// evaluation pipeline, over a fake IDatabaseExecutor — so what is asserted is the generated SQL
/// (for List) and the returned entity (for GetByKey), which is exactly where scoping either
/// happens or doesn't. Mirrors EmployeeRbacTests' style: no mocking library, seeding identical to
/// EmployeeModuleInitializationService.
/// </summary>
public sealed class EmployeeRowScopeTests
{
    private const string DepartmentFilterClause = "Department = @Filter_Department";
    private const string DepartmentFilterParameter = "Filter_Department";

    private static Employee EmployeeIn(string department) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeCode = "E001",
        Name = "Ada Lovelace",
        Email = "ada@example.com",
        Department = department,
        IsActive = true
    };

    // ---- List: the filter reaches the WHERE clause, so out-of-scope rows are never read ----

    [Fact]
    public async Task ScopedUser_ListAsync_FiltersByOwnDepartment()
    {
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => new object[] { EmployeeIn("Engineering") };

        await host.CrudEngine.ListAsync();

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains(DepartmentFilterClause, call.Sql);
        Assert.Equal("Engineering", call.Parameters![DepartmentFilterParameter]);
    }

    [Fact]
    public async Task UnscopedUser_ListAsync_HasNoDepartmentFilter()
    {
        // employee-admin holds employee.read.all, so OwnDepartmentAsync returns RowFilterDescriptor.None.
        var host = new RowScopeTestHost().SignIn("user-123", RowScopeTestHost.AdminRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => new object[] { EmployeeIn("Engineering"), EmployeeIn("Sales") };

        var result = await host.CrudEngine.ListAsync();

        Assert.Equal(2, result.Count);
        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Equal("SELECT * FROM Employees", call.Sql);
    }

    [Fact]
    public async Task EntityWithNoRowRule_ListAsync_IsUnaffected()
    {
        var host = new RowScopeTestHost(attachRowRule: false)
            .SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync();

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Equal("SELECT * FROM Employees", call.Sql);
    }

    [Fact]
    public async Task ScopedUser_WithNoDepartmentValue_FiltersToNull_SoMatchesNothing()
    {
        // Fail-closed: a scoped user who was never assigned a department must see zero rows, not
        // every row. `Department = NULL` matches nothing under SQL's three-valued logic.
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId);
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync();

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains(DepartmentFilterClause, call.Sql);
        Assert.Null(call.Parameters![DepartmentFilterParameter]);
    }

    [Fact]
    public async Task CallerSuppliedDepartmentFilter_CannotOverrideTheScopeFilter()
    {
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync(filters: new Dictionary<string, object?> { ["Department"] = "Sales" });

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Equal("Engineering", call.Parameters![DepartmentFilterParameter]);
    }

    [Fact]
    public async Task ScopeFilter_ComposesWithCallerFilterOnAnotherField()
    {
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => Array.Empty<object>();

        await host.CrudEngine.ListAsync(filters: new Dictionary<string, object?> { ["EmployeeCode"] = "E001" });

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("EmployeeCode = @Filter_EmployeeCode", call.Sql);
        Assert.Contains(DepartmentFilterClause, call.Sql);
    }

    // ---- GetByKey: an out-of-scope row is discarded, so the caller sees a 404, not a 403 ----

    [Fact]
    public async Task ScopedUser_GetAsync_InOwnDepartment_ReturnsTheRow()
    {
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        var employee = EmployeeIn("Engineering");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Same(employee, result);
    }

    [Fact]
    public async Task ScopedUser_GetAsync_OutsideOwnDepartment_ReturnsNull()
    {
        // Null is what EmployeesController turns into 404 — deliberately indistinguishable from a
        // nonexistent id, so scoping doesn't leak that the row exists.
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        var employee = EmployeeIn("Sales");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Null(result);
    }

    [Fact]
    public async Task ScopedUser_GetAsync_DepartmentComparison_IsCaseInsensitive()
    {
        // SQL Server's default collation is case-insensitive; the in-memory check GetByKey uses
        // must agree with the WHERE clause List generates, or the two paths would disagree.
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "engineering");
        var employee = EmployeeIn("Engineering");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Same(employee, result);
    }

    [Fact]
    public async Task UnscopedUser_GetAsync_ReturnsAnyDepartmentsRow()
    {
        var host = new RowScopeTestHost().SignIn("user-123", RowScopeTestHost.AdminRoleId, department: "Engineering");
        var employee = EmployeeIn("Sales");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Same(employee, result);
    }

    [Fact]
    public async Task ScopedUser_WithNoDepartmentValue_GetAsync_ReturnsNull()
    {
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId);
        host.Executor.OnQuerySingleOrDefault = _ => EmployeeIn("Engineering");

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = Guid.NewGuid() });

        Assert.Null(result);
    }

    // ---- Writes are covered transitively: the controller loads through GetAsync first ----

    [Fact]
    public async Task UpdateItself_IsNotRowScoped_SoTheControllersPreLoadIsWhatEnforcesIt()
    {
        // Documents the real boundary rather than a wish: RowScopeCrudHook does not filter the
        // Update operation. EmployeesController.Update calls GetAsync first (asserted by the
        // GetByKey tests above), which 404s an out-of-scope id before this path is ever reached.
        var host = new RowScopeTestHost().SignIn("user-456", RowScopeTestHost.ViewerRoleId, department: "Engineering");
        var employee = EmployeeIn("Sales");

        var result = await host.CrudEngine.UpdateAsync(employee);

        Assert.True(result.Succeeded);
    }

    // ---- The filter delegate's own contract ----

    [Fact]
    public void OwnDepartmentFilter_IsRegisteredUnderTheKeyTheSeededRuleNames()
    {
        var host = new RowScopeTestHost();

        var registry = host.Services.GetRequiredService<IRowFilterRegistry>();

        Assert.True(registry.TryResolve(EmployeeRowFilters.OwnDepartment, out var builder));
        Assert.NotNull(builder);
    }
}
