using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using APIPlatform.Rbac.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Rbac.Tests.Services;

/// <summary>End-to-end field mask through IFieldAuthorizationService -> PermissionEvaluator ->
/// ExecutionStage -> FieldMaskDescriptor.FromRules, the same call FieldMaskDescriptorTests
/// exercises directly.</summary>
public class FieldAuthorizationServiceTests
{
    [Fact]
    public async Task GetFieldMaskAsync_MixesGrantedAndUngrantedFields()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

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
            PermissionKey = "Widget.Read",
            Effect = PermissionEffect.Allow
        });

        var fieldStore = sp.GetRequiredService<IFieldPermissionRuleStore>();
        await fieldStore.AddRuleAsync(RbacTestHost.DefaultTenantId, new FieldPermissionRule
        {
            EntityKey = "Widget", FieldKey = "Price", PermissionKey = "Widget.Read", Access = FieldAccess.Read
        });
        await fieldStore.AddRuleAsync(RbacTestHost.DefaultTenantId, new FieldPermissionRule
        {
            EntityKey = "Widget", FieldKey = "Cost", PermissionKey = "Widget.ViewCost", Access = FieldAccess.Read
        });

        var mask = await sp.GetRequiredService<IFieldAuthorizationService>().GetFieldMaskAsync("Widget");

        Assert.Equal(FieldAccess.Read, mask.FieldAccess["Price"]);
        Assert.Equal(FieldAccess.None, mask.FieldAccess["Cost"]);
    }
}
