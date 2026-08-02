namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Platform-neutral abstraction over the authenticated caller. Deliberately avoids exposing
/// ASP.NET Core types (e.g. ClaimsPrincipal) so Foundation never locks the platform to a
/// specific host or auth pipeline. Claims carry arbitrary caller attributes (department, role,
/// email, language, timezone, etc.) that Workflow, Rbac, Notification, Search, and Audit can
/// each read only what they need from.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }

    /// <summary>All claims for the current user, keyed by claim type. Use <see cref="Constants.WellKnownClaimTypes"/> for common keys.</summary>
    IReadOnlyDictionary<string, string> Claims { get; }

    /// <summary>Convenience lookup over <see cref="Claims"/>; returns null if the claim isn't present. Use `GetClaim(type) is not null` to check existence.</summary>
    string? GetClaim(string claimType);
}
