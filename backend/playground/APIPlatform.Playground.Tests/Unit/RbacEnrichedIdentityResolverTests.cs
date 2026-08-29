using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Playground.Resolvers;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.DependencyInjection;
using APIPlatform.Rbac.Models;
using APIPlatform.Rbac.Stores;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APIPlatform.Playground.Tests.Unit;

/// <summary>
/// Proves the Phase 0 identity&lt;-&gt;RBAC bridge: RbacEnrichedIdentityResolver must fill
/// UserInfo.RoleIds/PermissionIds from live APIPlatform.Rbac grants, since neither
/// LoginsIdentityResolver nor PlaygroundIdentityResolver populate them, and ClaimsBuilder emits
/// the JWT's "role"/"permission" claims straight from whatever IIdentityResolver returns.
/// </summary>
public sealed class RbacEnrichedIdentityResolverTests
{
    private const string TenantId = HttpCurrentUserContextAdapter.TestTenantId;

    private static (RbacEnrichedIdentityResolver Resolver, InMemoryRoleStore Store, InMemoryUserScopeStore Scopes) BuildSubject(IIdentityResolver inner)
    {
        var services = new ServiceCollection();
        services.AddRbac();
        var provider = services.BuildServiceProvider();

        var store = (InMemoryRoleStore)provider.GetRequiredService<IRoleStore>();
        var scopes = new InMemoryUserScopeStore();
        var resolver = new RbacEnrichedIdentityResolver(
            inner,
            provider.GetRequiredService<IRoleService>(),
            provider.GetRequiredService<IPermissionResolver>(),
            scopes);

        return (resolver, store, scopes);
    }

    private static UserInfo BareUser(string userId, string username) => new()
    {
        UserId = userId,
        Username = username,
        PasswordHash = "irrelevant",
        IsActive = true,
        IsLocked = false
        // RoleIds/PermissionIds deliberately left at their empty defaults, matching what
        // LoginsIdentityResolver/PlaygroundIdentityResolver actually return today.
    };

    private sealed class StubInnerResolver : IIdentityResolver
    {
        private readonly UserInfo? _user;
        public StubInnerResolver(UserInfo? user) => _user = user;

        public Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_user);

        public Task<UserInfo?> ResolveByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_user);
    }

    [Fact]
    public async Task ResolveAsync_FillsRoleIdsAndPermissionIds_FromLiveRbacGrants()
    {
        var (resolver, store, _) = BuildSubject(new StubInnerResolver(BareUser("user-123", "admin")));

        store.SeedRole(new Role { Id = "employee-admin", Name = "Employee Administrator", TenantId = TenantId });
        await store.AssignRoleAsync(TenantId, "user-123", "employee-admin");
        await store.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = TenantId,
            RoleId = "employee-admin",
            PermissionKey = "employee.read",
            Effect = PermissionEffect.Allow
        });

        var result = await resolver.ResolveAsync("admin", null);

        Assert.NotNull(result);
        Assert.Contains("employee-admin", result!.RoleIds);
        Assert.Contains("employee.read", result.PermissionIds);
    }

    [Fact]
    public async Task ResolveByIdAsync_FillsRoleIdsAndPermissionIds_SameAsResolveAsync()
    {
        var (resolver, store, _) = BuildSubject(new StubInnerResolver(BareUser("user-456", "viewer")));

        store.SeedRole(new Role { Id = "employee-viewer", Name = "Employee Viewer", TenantId = TenantId });
        await store.AssignRoleAsync(TenantId, "user-456", "employee-viewer");
        await store.GrantPermissionAsync(new PermissionGrant
        {
            TenantId = TenantId,
            RoleId = "employee-viewer",
            PermissionKey = "employee.read",
            Effect = PermissionEffect.Allow
        });

        var result = await resolver.ResolveByIdAsync("user-456");

        Assert.NotNull(result);
        Assert.Contains("employee-viewer", result!.RoleIds);
        Assert.Contains("employee.read", result.PermissionIds);
    }

    [Fact]
    public async Task ResolveAsync_UserWithNoGrants_ReturnsEmptyRoleAndPermissionIds_NotDenied()
    {
        var (resolver, _, _) = BuildSubject(new StubInnerResolver(BareUser("someone-with-no-role", "ghost")));

        var result = await resolver.ResolveAsync("ghost", null);

        // The resolver's job is only to report what's true, not to gate login — CrudAuthorization
        // still default-denies this user's actual API calls (see EmployeeRbacTests).
        Assert.NotNull(result);
        Assert.Empty(result!.RoleIds);
        Assert.Empty(result.PermissionIds);
    }

    [Fact]
    public async Task ResolveAsync_InnerResolverReturnsNull_PropagatesNullWithoutCallingRbac()
    {
        var (resolver, _, _) = BuildSubject(new StubInnerResolver(null));

        var result = await resolver.ResolveAsync("no-such-user", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_FillsDepartmentId_FromTheUserScopeStore()
    {
        // Phase 2: [Logins] has no department column, so without this the JWT never carried a
        // department_id claim and the UI had no way to show a user their own scope. Enforcement
        // reads the same store live per request; this only affects what the token says.
        var (resolver, _, scopes) = BuildSubject(new StubInnerResolver(BareUser("user-456", "viewer")));
        await scopes.SetScopeAsync(TenantId, "user-456", ScopeKeys.Department, "Engineering");

        var result = await resolver.ResolveAsync("viewer", null);

        Assert.NotNull(result);
        Assert.Equal("Engineering", result!.DepartmentId);
    }

    [Fact]
    public async Task ResolveAsync_UserWithNoScopeRow_LeavesDepartmentIdNull()
    {
        var (resolver, _, _) = BuildSubject(new StubInnerResolver(BareUser("user-456", "viewer")));

        var result = await resolver.ResolveAsync("viewer", null);

        Assert.NotNull(result);
        Assert.Null(result!.DepartmentId);
    }

    [Fact]
    public async Task ResolveAsync_PreservesFieldsUntouchedByEnrichment()
    {
        var user = new UserInfo
        {
            UserId = "user-123",
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true,
            IsLocked = false,
            TenantId = "tenant-x"
        };
        var (resolver, _, _) = BuildSubject(new StubInnerResolver(user));

        var result = await resolver.ResolveAsync("admin", null);

        Assert.NotNull(result);
        Assert.Equal("admin@example.com", result!.Email);
        Assert.Equal("tenant-x", result.TenantId);
    }
}
