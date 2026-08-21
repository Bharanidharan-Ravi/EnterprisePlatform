using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.Playground.Tests.TestSupport;

/// <summary>Test double for the ICurrentUser/ITenantContext pair CrudEngine&lt;T&gt; and Rbac's
/// DefaultAuthorizationContextFactory both depend on — mirrors what
/// HttpCurrentUserContextAdapter provides at runtime, without needing a real HTTP request.</summary>
internal sealed class FakeCurrentUser : ICurrentUser, ITenantContext
{
    public string? UserId { get; set; } = "user-123";
    public string? UserName { get; set; } = "admin";
    public bool IsAuthenticated { get; set; } = true;
    public IReadOnlyDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    public string? GetClaim(string claimType) => Claims.TryGetValue(claimType, out var value) ? value : null;

    public string? TenantId { get; set; } = "default";
    public string? TenantCode => null;
    public bool HasTenant => true;
    public bool IsMultiTenant => false;
}
