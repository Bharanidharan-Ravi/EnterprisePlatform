using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using APIPlatform.Rbac.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Rbac.Tests.Services;

/// <summary>
/// Exercises PermissionEvaluator end-to-end through ICrudAuthorizationService — the same path
/// Nucleus.TestHarness.Rbac proves by hand — covering allow, deny (no grant), and deny-overrides-
/// allow precedence (PermissionResolver.ResolveAsync).
/// </summary>
public class CrudAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_WithMatchingRoleGrant_Allows()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedRoleAndGrantAsync(sp, "Widget.Read", PermissionEffect.Allow);

        var result = await sp.GetRequiredService<ICrudAuthorizationService>().AuthorizeAsync("Widget", "Read");

        Assert.True(result.Allowed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task AuthorizeAsync_WithNoGrant_Denies()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICrudAuthorizationService>()
            .AuthorizeAsync("Widget", "Delete");

        Assert.False(result.Allowed);
        Assert.Equal("No matching permission grant for the required permission key(s).", result.Reason);
    }

    [Fact]
    public async Task AuthorizeAsync_UserLevelDenyOverridesRoleLevelAllow_ForSameKey()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedRoleAndGrantAsync(sp, "Widget.Read", PermissionEffect.Allow);

        var roleService = sp.GetRequiredService<IRoleService>();
        await roleService.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = RbacTestHost.DefaultTenantId,
            UserId = RbacTestHost.DefaultUserId,
            PermissionKey = "Widget.Read",
            Effect = PermissionEffect.Deny
        });

        var result = await sp.GetRequiredService<ICrudAuthorizationService>().AuthorizeAsync("Widget", "Read");

        Assert.False(result.Allowed);
    }

    /// <summary>Seeds the Role record InMemoryRoleStore requires (see Nucleus.TestHarness.Rbac
    /// Program.cs remarks), assigns it to the default user, and grants one permission on it.</summary>
    private static async Task SeedRoleAndGrantAsync(IServiceProvider sp, string permissionKey, PermissionEffect effect)
    {
        if (sp.GetRequiredService<IRoleStore>() is InMemoryRoleStore roleStore)
        {
            roleStore.SeedRole(new Role { Id = "role-editor", Name = "Editor", TenantId = RbacTestHost.DefaultTenantId });
        }

        var roleService = sp.GetRequiredService<IRoleService>();
        await roleService.AssignRoleAsync(RbacTestHost.DefaultTenantId, RbacTestHost.DefaultUserId, "role-editor");
        await roleService.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = RbacTestHost.DefaultTenantId,
            RoleId = "role-editor",
            PermissionKey = permissionKey,
            Effect = effect
        });
    }
}
