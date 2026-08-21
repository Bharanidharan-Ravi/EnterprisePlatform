using APIPlatform.Foundation.Interfaces;
using APIPlatform.Playground.Tests.TestSupport;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.DependencyInjection;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Playground.Tests.Unit;

/// <summary>
/// UNIT tests proving phase2.md 22's RBAC acceptance criteria: an allowed user's read/update
/// succeed, a denied user's are rejected — using ICrudAuthorizationService directly (no ASP.NET
/// Core policy/handler plumbing is needed; Rbac has zero ASP.NET Core dependency). Seeding
/// mirrors EmployeeModuleInitializationService exactly (admin=full CRUD, viewer=read-only).
/// </summary>
public sealed class EmployeeRbacTests
{
    private const string TenantId = "default";
    private const string EntityKey = "employee";

    private static (ServiceProvider Services, FakeCurrentUser CurrentUser) BuildHost(string userId)
    {
        var currentUser = new FakeCurrentUser { UserId = userId, TenantId = TenantId };

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(currentUser);
        services.AddSingleton<ITenantContext>(currentUser);
        services.AddRbac();

        var provider = services.BuildServiceProvider();

        var store = (InMemoryRoleStore)provider.GetRequiredService<IRoleStore>();
        store.SeedRole(new Role { Id = "employee-admin", Name = "Employee Administrator", TenantId = TenantId });
        store.SeedRole(new Role { Id = "employee-viewer", Name = "Employee Viewer", TenantId = TenantId });

        store.AssignRoleAsync(TenantId, "user-123", "employee-admin").GetAwaiter().GetResult();
        foreach (var action in new[] { "read", "create", "update", "delete" })
        {
            store.GrantPermissionAsync(new PermissionGrant
            {
                TenantId = TenantId,
                RoleId = "employee-admin",
                PermissionKey = $"{EntityKey}.{action}",
                Effect = PermissionEffect.Allow
            }).GetAwaiter().GetResult();
        }

        store.AssignRoleAsync(TenantId, "user-456", "employee-viewer").GetAwaiter().GetResult();
        store.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = TenantId,
            RoleId = "employee-viewer",
            PermissionKey = $"{EntityKey}.read",
            Effect = PermissionEffect.Allow
        }).GetAwaiter().GetResult();

        return (provider, currentUser);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task AdminUser_IsAllowed_ForEveryAction(string action)
    {
        var (services, _) = BuildHost("user-123");
        var authz = services.GetRequiredService<ICrudAuthorizationService>();

        var result = await authz.AuthorizeAsync(EntityKey, action);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task ViewerUser_IsAllowed_ForRead()
    {
        var (services, _) = BuildHost("user-456");
        var authz = services.GetRequiredService<ICrudAuthorizationService>();

        var result = await authz.AuthorizeAsync(EntityKey, "read");

        Assert.True(result.Allowed);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task ViewerUser_IsDenied_ForWriteActions(string action)
    {
        var (services, _) = BuildHost("user-456");
        var authz = services.GetRequiredService<ICrudAuthorizationService>();

        var result = await authz.AuthorizeAsync(EntityKey, action);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task UnknownUser_IsDenied_ByDefault()
    {
        var (services, _) = BuildHost("someone-with-no-role");
        var authz = services.GetRequiredService<ICrudAuthorizationService>();

        var result = await authz.AuthorizeAsync(EntityKey, "read");

        Assert.False(result.Allowed); // RbacOptions.DefaultDeny = true
    }
}
