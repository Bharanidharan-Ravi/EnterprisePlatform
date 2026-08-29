using APIPlatform.CrudEngine.Models;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Tests.TestSupport;
using Xunit;

namespace APIPlatform.Playground.Tests.Unit;

/// <summary>
/// UNIT tests for field-level masking: Email is visible only to a role holding
/// EmployeeFieldMasks.EmailAccessPermissionKey (employee-admin). Drives the real
/// ICrudEngine&lt;Employee&gt; through the real FieldMaskCrudHook and the real Rbac evaluation
/// pipeline over a fake IDatabaseExecutor, so what's asserted is the actual entity/list
/// FieldMaskCrudHook hands back — exactly where masking either happens or doesn't. Mirrors
/// EmployeeRowScopeTests' style.
/// </summary>
public sealed class EmployeeFieldMaskTests
{
    private static Employee EmployeeWithEmail(string email) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeCode = "E001",
        Name = "Ada Lovelace",
        Email = email,
        Department = "Engineering",
        IsActive = true
    };

    // ---- GetByKey ----

    [Fact]
    public async Task AdminUser_GetAsync_SeesEmail()
    {
        var host = new FieldMaskTestHost().SignIn("user-123", FieldMaskTestHost.AdminRoleId);
        var employee = EmployeeWithEmail("ada@example.com");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Equal("ada@example.com", result!.Email);
    }

    [Fact]
    public async Task ViewerUser_GetAsync_EmailIsNulledOut()
    {
        var host = new FieldMaskTestHost().SignIn("user-456", FieldMaskTestHost.ViewerRoleId);
        var employee = EmployeeWithEmail("ada@example.com");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        // Mutates the same instance the fake executor handed back — proves the real value never
        // reaches whatever builds the HTTP response, not just that a copy is filtered.
        Assert.Same(employee, result);
        Assert.Null(result!.Email);
    }

    [Fact]
    public async Task ViewerUser_GetAsync_OtherFieldsAreUntouched()
    {
        var host = new FieldMaskTestHost().SignIn("user-456", FieldMaskTestHost.ViewerRoleId);
        var employee = EmployeeWithEmail("ada@example.com");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Equal("E001", result!.EmployeeCode);
        Assert.Equal("Ada Lovelace", result.Name);
        Assert.Equal("Engineering", result.Department);
    }

    // ---- List ----

    [Fact]
    public async Task AdminUser_ListAsync_SeesEmailOnEveryRow()
    {
        var host = new FieldMaskTestHost().SignIn("user-123", FieldMaskTestHost.AdminRoleId);
        host.Executor.OnQuery = _ => new object[] { EmployeeWithEmail("a@x.com"), EmployeeWithEmail("b@x.com") };

        var result = await host.CrudEngine.ListAsync();

        Assert.All(result, e => Assert.NotNull(e.Email));
    }

    [Fact]
    public async Task ViewerUser_ListAsync_EmailIsNulledOutOnEveryRow()
    {
        var host = new FieldMaskTestHost().SignIn("user-456", FieldMaskTestHost.ViewerRoleId);
        host.Executor.OnQuery = _ => new object[] { EmployeeWithEmail("a@x.com"), EmployeeWithEmail("b@x.com") };

        var result = await host.CrudEngine.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Null(e.Email));
    }

    // ---- Absence of a rule means no restriction (FieldPermissionRule's own documented default) ----

    [Fact]
    public async Task EntityWithNoFieldRule_GetAsync_IsUnaffected()
    {
        var host = new FieldMaskTestHost(attachFieldRule: false).SignIn("user-456", FieldMaskTestHost.ViewerRoleId);
        var employee = EmployeeWithEmail("ada@example.com");
        host.Executor.OnQuerySingleOrDefault = _ => employee;

        var result = await host.CrudEngine.GetAsync(new EntityKeyValues { ["Id"] = employee.Id });

        Assert.Equal("ada@example.com", result!.Email);
    }

    // ---- Composes with Phase 2 row scoping on the same call ----

    [Fact]
    public async Task RowScopedUser_ListAsync_IsBothFilteredByRowAndMaskedByField()
    {
        var host = new FieldMaskTestHost(attachRowRule: true)
            .SignIn("user-456", FieldMaskTestHost.ViewerRoleId, department: "Engineering");
        host.Executor.OnQuery = _ => new object[] { EmployeeWithEmail("ada@example.com") };

        var result = await host.CrudEngine.ListAsync();

        var call = Assert.Single(host.Executor.QueryCalls);
        Assert.Contains("Department = @Filter_Department", call.Sql); // row scope reached the WHERE clause
        Assert.Equal("Engineering", call.Parameters!["Filter_Department"]);
        var employee = Assert.Single(result);
        Assert.Null(employee.Email); // field mask still applied to what came back
    }
}
