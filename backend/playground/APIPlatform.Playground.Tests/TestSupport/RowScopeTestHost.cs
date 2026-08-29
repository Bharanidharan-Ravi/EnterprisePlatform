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
/// EmployeeTestHost's RBAC-aware sibling: the same CrudEngine graph over a FakeDatabaseExecutor,
/// but with AddRbac() and the Phase 2 row-scoping wiring on top (RowScopeCrudHook + the
/// OwnDepartment filter delegate) — i.e. exactly what AddEmployeeModule() composes at runtime,
/// minus SQL Server and ASP.NET Core hosting. Scope-claim merging itself needs no test-host wiring
/// at all: it's Rbac's own DefaultAuthorizationContextFactory now, exercised through plain
/// AddRbac() the same as everything else here.
///
/// Registration ORDER matters here and mirrors the real composition root: IUserScopeStore goes in
/// before AddRbac(), so Rbac's TryAddSingleton default (InMemoryUserScopeStore) is skipped in favor
/// of the same instance this host controls directly via <see cref="Scopes"/>.
/// </summary>
internal sealed class RowScopeTestHost
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

    /// <param name="attachRowRule">false leaves Employee with no RowPermissionRule at all — the
    /// "entity nobody scoped" case, which must behave exactly as it did before Phase 2.</param>
    public RowScopeTestHost(bool attachRowRule = true)
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
        services.AddCrudPipelineHook<RowScopeCrudHook>();

        Services = services.BuildServiceProvider();

        EmployeeRowFilters.RegisterAll(Services.GetRequiredService<IRowFilterRegistry>());
        SeedRbac(attachRowRule);
    }

    /// <summary>Mirrors EmployeeModuleInitializationService: admin holds full CRUD plus the
    /// unscoped-read escape hatch; viewer holds read only and is therefore row-scoped.</summary>
    private void SeedRbac(bool attachRowRule)
    {
        var roleStore = (InMemoryRoleStore)Services.GetRequiredService<IRoleStore>();
        roleStore.SeedRole(new Role { Id = AdminRoleId, Name = "Employee Administrator", TenantId = TenantId });
        roleStore.SeedRole(new Role { Id = ViewerRoleId, Name = "Employee Viewer", TenantId = TenantId });

        Grant(roleStore, AdminRoleId, $"{EntityKey}.read");
        Grant(roleStore, AdminRoleId, $"{EntityKey}.update");
        Grant(roleStore, AdminRoleId, EmployeeRowFilters.UnscopedReadPermissionKey);
        Grant(roleStore, ViewerRoleId, $"{EntityKey}.read");

        if (!attachRowRule) return;

        Services.GetRequiredService<IRowPermissionRuleStore>()
            .AddRuleAsync(TenantId, new RowPermissionRule
            {
                EntityKey = EntityKey,
                FilterDelegateKey = EmployeeRowFilters.OwnDepartment
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

    /// <summary>Signs the given user in, in the role named, optionally scoped to a department.</summary>
    public RowScopeTestHost SignIn(string userId, string roleId, string? department = null)
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
