using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Resolves a UserInfo from a login identifier. Apps supply this — Nucleus never
/// queries a specific User table. One line of DI: AddScoped&lt;IIdentityResolver, MyUserResolver&gt;().
/// </summary>
public interface IIdentityResolver
{
    Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default);

    /// <summary>Re-resolves a user by their stable Id (UserInfo.UserId) rather than a login
    /// identifier — used by refresh-token rotation, where the caller only ever supplies the Id
    /// (never a password, so ResolveAsync's identifier/tenant lookup doesn't apply). Return the
    /// same UserInfo shape ResolveAsync would for that user, or null if the account no longer
    /// exists or should no longer be allowed to authenticate.</summary>
    Task<UserInfo?> ResolveByIdAsync(string userId, CancellationToken cancellationToken = default);
}
