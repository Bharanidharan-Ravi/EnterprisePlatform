using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Logging.Abstractions;
using APIPlatform.Playground.Metadata;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APIPlatform.Playground.Services;

/// <summary>
/// Startup wiring for the Phase 2 Employee module: (1) runs the platform's existing migration
/// engine (IMigrationRunner — idempotent, history-tracked, the same mechanism
/// DatabaseMigrationController's manual POST /run endpoint uses) so the Employees table exists
/// before the API is first called, and (2) seeds RBAC test data via IRoleStore.
/// TEST ONLY: hardcoded roles/grants for the two hardcoded PlaygroundIdentityResolver users,
/// purely to prove RBAC allow/deny (phase2.md 22) — never a pattern for a real deployment,
/// which must supply its own durable IRoleStore (see InMemoryRoleStore's own warning).
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
        if (roleStore is not InMemoryRoleStore store)
        {
            // A real IRoleStore was supplied — this TEST ONLY seeding only knows how to talk to
            // the default in-memory one; skip rather than guess at a durable store's API.
            return;
        }

        const string tenantId = Infrastructure.HttpCurrentUserContextAdapter.TestTenantId;
        const string entityKey = EmployeeEntityDefinitionProvider.EntityName;

        store.SeedRole(new Role { Id = AdminRoleId, Name = "Employee Administrator", TenantId = tenantId });
        store.SeedRole(new Role { Id = ViewerRoleId, Name = "Employee Viewer", TenantId = tenantId });

        // admin (PlaygroundIdentityResolver user-123) -> full CRUD
        await store.AssignRoleAsync(tenantId, "user-123", AdminRoleId, cancellationToken);
        foreach (var action in new[] { "read", "create", "update", "delete" })
        {
            await store.GrantPermissionAsync(new PermissionGrant
            {
                TenantId = tenantId,
                RoleId = AdminRoleId,
                PermissionKey = $"{entityKey.ToLowerInvariant()}.{action}",
                Effect = PermissionEffect.Allow
            }, cancellationToken);
        }

        // viewer (PlaygroundIdentityResolver user-456) -> read only; create/update/delete are
        // denied purely by absence of a grant (RbacOptions.DefaultDeny = true).
        await store.AssignRoleAsync(tenantId, "user-456", ViewerRoleId, cancellationToken);
        await store.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = tenantId,
            RoleId = ViewerRoleId,
            PermissionKey = $"{entityKey.ToLowerInvariant()}.read",
            Effect = PermissionEffect.Allow
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
