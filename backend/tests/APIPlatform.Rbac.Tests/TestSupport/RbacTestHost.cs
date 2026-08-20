using APIPlatform.Foundation.Interfaces;
using APIPlatform.Rbac.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Rbac.Tests.TestSupport;

/// <summary>
/// Builds a fully-wired Rbac service provider the same way a consuming app would (services.AddRbac()
/// on top of app-supplied ICurrentUser/ITenantContext) rather than hand-constructing pipeline stages
/// — mirrors Nucleus.TestHarness.Rbac's Program.cs so tests exercise the real registration graph.
/// </summary>
internal static class RbacTestHost
{
    public const string DefaultTenantId = "tenant-1";
    public const string DefaultUserId = "user-1";

    public static ServiceProvider Build(string tenantId = DefaultTenantId, string userId = DefaultUserId)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(userId));
        services.AddSingleton<ITenantContext>(new FakeTenantContext(tenantId));
        services.AddRbac();
        return services.BuildServiceProvider();
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(string userId) => UserId = userId;
        public string? UserId { get; }
        public string? UserName => UserId;
        public bool IsAuthenticated => true;
        public IReadOnlyDictionary<string, string> Claims { get; } = new Dictionary<string, string>();
        public string? GetClaim(string claimType) => Claims.TryGetValue(claimType, out var value) ? value : null;
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(string tenantId) => TenantId = tenantId;
        public string? TenantId { get; }
        public string? TenantCode => TenantId;
        public bool HasTenant => TenantId is not null;
        public bool IsMultiTenant => true;
    }
}
