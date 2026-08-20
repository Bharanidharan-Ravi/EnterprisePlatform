// Smoke-test proving APIPlatform.Rbac end-to-end (Master Plan Hard Rule 5: no module is
// "done" without Test Harness proof). Scoped to Rbac alone — Auth/CrudEngine aren't built in
// this project yet, so ICurrentUser/ITenantContext are satisfied here with trivial stand-ins.

using APIPlatform.Foundation.Interfaces;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.DependencyInjection;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<ICurrentUser>(new HarnessCurrentUser("user-1"));
services.AddSingleton<ITenantContext>(new HarnessTenantContext("tenant-1"));
services.AddRbac();

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;

// InMemoryRoleStore.GetEffectiveRolesForUserAsync only returns roles that have a seeded Role
// record (it resolves user<->role assignments against _roles for hierarchy/metadata) —
// AssignRoleAsync alone records the assignment tuple but not the Role itself.
if (sp.GetRequiredService<IRoleStore>() is InMemoryRoleStore roleStore)
{
    roleStore.SeedRole(new Role { Id = "role-editor", Name = "Editor", TenantId = "tenant-1" });
}

var roleService = sp.GetRequiredService<IRoleService>();
await roleService.AssignRoleAsync("tenant-1", "user-1", "role-editor");
await roleService.GrantPermissionAsync(new PermissionGrant
{
    TenantId = "tenant-1",
    RoleId = "role-editor",
    PermissionKey = "Widget.Read",
    Effect = PermissionEffect.Allow
});

var crudAuth = sp.GetRequiredService<ICrudAuthorizationService>();

var allowed = await crudAuth.AuthorizeAsync("Widget", "Read");
Console.WriteLine($"Widget.Read  -> Allowed={allowed.Allowed}  Reason={allowed.Reason ?? "(none)"}");

var denied = await crudAuth.AuthorizeAsync("Widget", "Delete");
Console.WriteLine($"Widget.Delete -> Allowed={denied.Allowed}  Reason={denied.Reason}");

sealed class HarnessCurrentUser : ICurrentUser
{
    public HarnessCurrentUser(string userId) => UserId = userId;
    public string? UserId { get; }
    public string? UserName => UserId;
    public bool IsAuthenticated => true;
    public IReadOnlyDictionary<string, string> Claims { get; } = new Dictionary<string, string>();
    public string? GetClaim(string claimType) => Claims.TryGetValue(claimType, out var value) ? value : null;
}

sealed class HarnessTenantContext : ITenantContext
{
    public HarnessTenantContext(string tenantId) => TenantId = tenantId;
    public string? TenantId { get; }
    public string? TenantCode => TenantId;
    public bool HasTenant => TenantId is not null;
    public bool IsMultiTenant => true;
}
