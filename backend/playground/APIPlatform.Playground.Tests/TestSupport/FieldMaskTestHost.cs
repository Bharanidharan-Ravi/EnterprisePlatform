using APIPlatform.CrudEngine.DependencyInjection;
using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Engine;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Playground.Defaults;
using APIPlatform.Playground.Metadata;
using APIPlatform.Playground.Rbac;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.DependencyInjection;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Tests.TestSupport;

/// <summary>
/// RowScopeTestHost's field-masking sibling — the same CrudEngine + AddRbac() graph, with
/// FieldMaskCrudHook wired on top (and RowScopeCrudHook too, since AddEmployeeModule() always
/// registers both together; a composition test proves they don't interfere with each other).
/// </summary>
internal sealed class FieldMaskTestHost
{
    public const string TenantId = "default";
    public const string EntityKey = "employee";
    public const string AdminRoleId = "employee-admin";
    public const string ViewerRoleId = "employee-viewer";

    public FakeDatabaseExecutor Executor { get; } = new();
    public FakeClock Clock { get; } = new();
    public FakeCurrentUser CurrentUser { get; } = new();
    public InMemoryUserScopeStore Scopes { get; } = new();
    public ServiceProvider Services { get; }

    /// <param name="attachFieldRule">false leaves Employee.Email with no FieldPermissionRule at
    /// all — the "field nobody masked" case, which must behave exactly as it did before Phase 1.</param>
    /// <param name="attachRowRule">true also wires the Phase 2 OwnDepartment row rule, so a test
    /// can prove the two concerns compose on the same List/Get call.</param>
    public FieldMaskTestHost(bool attachFieldRule = true, bool attachRowRule = false)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<ITenantContext>(CurrentUser);
        services.AddSingleton<IDatabaseExecutor>(Executor);
        services.Configure<DatabaseOptions>(o =>
        {
            o.ConnectionString = "unused-in-unit-tests";
            o.Provider = DatabaseProvider.SqlServer;
        });

        services.AddSingleton<IEntityDefinitionProvider, EmployeeEntityDefinitionProvider>();
        services.AddSingleton<IEntityDefaultValueProvider, EmployeeDefaultValueProvider>();

        services.AddCrudEngine();

        services.AddSingleton<IUserScopeStore>(Scopes);
        services.AddRbac();
        services.AddCrudPipelineHook<FieldMaskCrudHook>();
        if (attachRowRule) services.AddCrudPipelineHook<RowScopeCrudHook>();

        Services = services.BuildServiceProvider();

        if (attachRowRule) EmployeeRowFilters.RegisterAll(Services.GetRequiredService<IRowFilterRegistry>());
        SeedRbac(attachFieldRule, attachRowRule);
    }

    /// <summary>Mirrors EmployeeModuleInitializationService: admin holds Email access (Write) via
    /// EmployeeFieldMasks.EmailAccessPermissionKey; viewer doesn't, so Email masks to None.</summary>
    private void SeedRbac(bool attachFieldRule, bool attachRowRule)
    {
        var roleStore = (InMemoryRoleStore)Services.GetRequiredService<IRoleStore>();
        roleStore.SeedRole(new Role { Id = AdminRoleId, Name = "Employee Administrator", TenantId = TenantId });
        roleStore.SeedRole(new Role { Id = ViewerRoleId, Name = "Employee Viewer", TenantId = TenantId });

        Grant(roleStore, AdminRoleId, $"{EntityKey}.read");
        Grant(roleStore, AdminRoleId, $"{EntityKey}.update");
        Grant(roleStore, AdminRoleId, EmployeeFieldMasks.EmailAccessPermissionKey);
        Grant(roleStore, ViewerRoleId, $"{EntityKey}.read");

        if (attachRowRule)
        {
            Grant(roleStore, AdminRoleId, EmployeeRowFilters.UnscopedReadPermissionKey);
            Services.GetRequiredService<IRowPermissionRuleStore>()
                .AddRuleAsync(TenantId, new RowPermissionRule { EntityKey = EntityKey, FilterDelegateKey = EmployeeRowFilters.OwnDepartment })
                .GetAwaiter().GetResult();
        }

        if (!attachFieldRule) return;

        Services.GetRequiredService<IFieldPermissionRuleStore>()
            .AddRuleAsync(TenantId, new FieldPermissionRule
            {
                EntityKey = EntityKey,
                FieldKey = EmployeeFieldMasks.EmailField,
                PermissionKey = EmployeeFieldMasks.EmailAccessPermissionKey,
                Access = FieldAccess.Write
            })
            .GetAwaiter().GetResult();
    }

    private static void Grant(IRoleStore store, string roleId, string permissionKey) =>
        store.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = TenantId,
            RoleId = roleId,
            PermissionKey = permissionKey,
            Effect = PermissionEffect.Allow
        }).GetAwaiter().GetResult();

    public FieldMaskTestHost SignIn(string userId, string roleId, string? department = null)
    {
        CurrentUser.UserId = userId;
        Services.GetRequiredService<IRoleStore>().AssignRoleAsync(TenantId, userId, roleId).GetAwaiter().GetResult();

        if (department is not null)
            Scopes.SetScopeAsync(TenantId, userId, ScopeKeys.Department, department).GetAwaiter().GetResult();

        return this;
    }

    public ICrudEngine<Playground.Models.Employee> CrudEngine =>
        Services.GetRequiredService<ICrudEngine<Playground.Models.Employee>>();
}
