using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Logging.Abstractions;
using APIPlatform.Playground.Metadata;
using APIPlatform.Playground.Rbac;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APIPlatform.Playground.Services;

/// <summary>
/// Startup wiring for the Phase 2 Employee module: (1) runs the platform's existing migration
/// engine (IMigrationRunner — idempotent, history-tracked, the same mechanism
/// DatabaseMigrationController's manual POST /run endpoint uses) so the Employees table (and,
/// since SqlServerRoleStore was wired in, the RBAC tables) exist before the API is first called,
/// and (2) seeds RBAC role/grant data via IRoleStore — works against either InMemoryRoleStore
/// (the Rbac default, synchronous SeedRole) or any store additionally implementing
/// IRoleDefinitionSeeder (SqlServerRoleStore); an IRoleStore that is neither is left alone, since
/// this TEST ONLY seeding doesn't know its API. Every write here is idempotent on the durable
/// path, since this runs again on every app start.
/// Also best-effort seeds the same two roles against whatever real [Logins] rows are named
/// "admin"/"viewer" (see SeedRealLoginsUserAsync) — the actual running app authenticates through
/// LoginsIdentityResolver, not PlaygroundIdentityResolver, so without this a real login always
/// lands with zero roles/permissions (RbacEnrichedIdentityResolver has nothing to enrich from).
/// </summary>
public sealed class EmployeeModuleInitializationService : IHostedService
{
    public const string AdminRoleId = "employee-admin";
    public const string ViewerRoleId = "employee-viewer";

    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformLogger<EmployeeModuleInitializationService> _logger;

    public EmployeeModuleInitializationService(IServiceProvider serviceProvider, IPlatformLogger<EmployeeModuleInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            var migrationResult = await migrationRunner.RunAsync(cancellationToken);
            _logger.LogInformation(
                "Employee module migrations: {Applied} applied, {Skipped} already applied.",
                migrationResult.Applied.Count, migrationResult.Skipped.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run Employee module migrations.");
        }

        try
        {
            await SeedRbacAsync(scope.ServiceProvider, cancellationToken);
            _logger.LogInformation("Employee module RBAC test data seeded (admin=full CRUD, viewer=read-only).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed Employee module RBAC test data.");
        }
    }

    private static async Task SeedRbacAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var roleStore = services.GetRequiredService<IRoleStore>();

        const string tenantId = Infrastructure.HttpCurrentUserContextAdapter.TestTenantId;
        const string entityKey = EmployeeEntityDefinitionProvider.EntityName;

        var adminRole = new Role { Id = AdminRoleId, Name = "Employee Administrator", TenantId = tenantId };
        var viewerRole = new Role { Id = ViewerRoleId, Name = "Employee Viewer", TenantId = tenantId };

        switch (roleStore)
        {
            case InMemoryRoleStore inMemory:
                inMemory.SeedRole(adminRole);
                inMemory.SeedRole(viewerRole);
                break;
            case IRoleDefinitionSeeder seeder:
                await seeder.EnsureRoleAsync(adminRole, cancellationToken);
                await seeder.EnsureRoleAsync(viewerRole, cancellationToken);
                break;
            default:
                // Neither shape this TEST ONLY seeding knows how to define a Role against —
                // skip rather than guess at an unknown durable store's API.
                return;
        }

        // admin (PlaygroundIdentityResolver user-123) -> full CRUD
        await roleStore.AssignRoleAsync(tenantId, "user-123", AdminRoleId, cancellationToken);
        foreach (var action in new[] { "read", "create", "update", "delete" })
        {
            await roleStore.GrantPermissionAsync(new PermissionGrant
            {
                TenantId = tenantId,
                RoleId = AdminRoleId,
                PermissionKey = $"{entityKey.ToLowerInvariant()}.{action}",
                Effect = PermissionEffect.Allow
            }, cancellationToken);
        }

        // viewer (PlaygroundIdentityResolver user-456) -> read only; create/update/delete are
        // denied purely by absence of a grant (RbacOptions.DefaultDeny = true).
        await roleStore.AssignRoleAsync(tenantId, "user-456", ViewerRoleId, cancellationToken);
        await roleStore.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = tenantId,
            RoleId = ViewerRoleId,
            PermissionKey = $"{entityKey.ToLowerInvariant()}.read",
            Effect = PermissionEffect.Allow
        }, cancellationToken);

        // Real [Logins]-table users, looked up by Username exactly like LoginsIdentityResolver
        // does — grants the same roles against whatever real Id comes back, so RbacEnrichedIdentityResolver
        // has something to enrich the JWT with for an actual login, not just the two hardcoded
        // PlaygroundIdentityResolver ids above. A username with no matching row is skipped, not an error.
        await SeedRealLoginsUserAsync(services, roleStore, tenantId, entityKey, "admin", AdminRoleId,
            new[] { "read", "create", "update", "delete" }, cancellationToken);
        await SeedRealLoginsUserAsync(services, roleStore, tenantId, entityKey, "viewer", ViewerRoleId,
            new[] { "read" }, cancellationToken);
    }

    private static async Task SeedRealLoginsUserAsync(
        IServiceProvider services,
        IRoleStore roleStore,
        string tenantId,
        string entityKey,
        string username,
        string roleId,
        IReadOnlyList<string> actions,
        CancellationToken cancellationToken)
    {
        var dynamicQuery = services.GetRequiredService<IDynamicQueryService>();
        var rows = await dynamicQuery.QueryAsync(new DynamicQueryRequest
        {
            TableName = "Logins",
            Columns = new[] { "Id", "Username" },
            Filters = new Dictionary<string, object?> { ["Username"] = username },
            Top = 1
        }, cancellationToken);

        var realUserId = rows.FirstOrDefault()?.GetValueOrDefault("Id")?.ToString();
        if (string.IsNullOrEmpty(realUserId)) return; // no such Logins row yet — nothing to seed, not an error

        await roleStore.AssignRoleAsync(tenantId, realUserId, roleId, cancellationToken);
        foreach (var action in actions)
        {
            await roleStore.GrantPermissionAsync(new PermissionGrant
            {
                TenantId = tenantId,
                RoleId = roleId,
                PermissionKey = $"{entityKey.ToLowerInvariant()}.{action}",
                Effect = PermissionEffect.Allow
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
