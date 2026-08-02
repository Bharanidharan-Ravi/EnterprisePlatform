namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Exposes only the currently resolved tenant. Resolution strategy (header, JWT, subdomain,
/// database lookup) is out of scope here and belongs to ITenantResolver in a future
/// APIPlatform.MultiTenancy package.
/// </summary>
public interface ITenantContext
{
    /// <summary>The resolved tenant identifier, or null if none has been resolved.</summary>
    string? TenantId { get; }

    /// <summary>A human-friendly tenant code, or null if none has been resolved.</summary>
    string? TenantCode { get; }

    /// <summary>True when a tenant has actually been resolved for the current context.</summary>
    bool HasTenant { get; }

    /// <summary>
    /// True when the running application is configured for multi-tenancy at all. Lets callers
    /// distinguish "single-tenant app, TenantId is null by design" from "multi-tenant app,
    /// no tenant resolved yet" — both otherwise look identical via <see cref="TenantId"/> alone.
    /// </summary>
    bool IsMultiTenant { get; }
}
