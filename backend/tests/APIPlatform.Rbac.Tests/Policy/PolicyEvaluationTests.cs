using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using APIPlatform.Rbac.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Rbac.Tests.Policy;

/// <summary>
/// Covers POLICY EVALUATION (ExecutionStage.EvaluatePoliciesAsync -> IPolicyEngine) on top of an
/// already-allowed permission grant — a granted key can still be denied by a failing policy, and
/// an unregistered policy name fails closed (PolicyEngine.cs remarks).
/// </summary>
public class PolicyEvaluationTests
{
    [Fact]
    public async Task AuthorizeAsync_GrantedKeyWithPassingPolicy_Allows()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedGrantAndPolicyAsync(sp, "always-allow", passes: true);

        var result = await sp.GetRequiredService<ICrudAuthorizationService>().AuthorizeAsync("Widget", "Read");

        Assert.True(result.Allowed);
        Assert.Single(result.AppliedPolicies);
    }

    [Fact]
    public async Task AuthorizeAsync_GrantedKeyWithFailingPolicy_Denies()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedGrantAndPolicyAsync(sp, "always-deny", passes: false);

        var result = await sp.GetRequiredService<ICrudAuthorizationService>().AuthorizeAsync("Widget", "Read");

        Assert.False(result.Allowed);
        Assert.Equal("One or more applicable policy rules denied access.", result.Reason);
    }

    [Fact]
    public async Task AuthorizeAsync_GrantedKeyWithUnregisteredPolicyName_FailsClosed()
    {
        using var provider = RbacTestHost.Build();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Register the PolicyRule against the tenant, but never register "missing-handler" in
        // IPolicyRuleRegistry — PolicyEngine.EvaluateAsync must treat that as a denial, not a pass.
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
        await sp.GetRequiredService<IRoleStore>().RegisterPolicyRuleAsync(RbacTestHost.DefaultTenantId, new PolicyRule
        {
            Name = "missing-handler",
            PermissionKey = "Widget.Read",
            ResourceType = ResourceType.Crud
        });

        var result = await sp.GetRequiredService<ICrudAuthorizationService>().AuthorizeAsync("Widget", "Read");

        Assert.False(result.Allowed);
    }

    private static async Task SeedGrantAndPolicyAsync(IServiceProvider sp, string policyName, bool passes)
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
            PermissionKey = "Widget.Read",
            Effect = PermissionEffect.Allow
        });

        sp.GetRequiredService<IPolicyRuleRegistry>().Register(policyName, (_, _) => Task.FromResult(passes));
        await sp.GetRequiredService<IRoleStore>().RegisterPolicyRuleAsync(RbacTestHost.DefaultTenantId, new PolicyRule
        {
            Name = policyName,
            PermissionKey = "Widget.Read",
            ResourceType = ResourceType.Crud
        });
    }
}
