using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Interfaces;

/// <summary>Resolves a UserInfo from a login identifier. Apps supply this — Nucleus never
/// queries a specific User table. One line of DI: AddScoped&lt;IIdentityResolver, MyUserResolver&gt;().
/// </summary>
public interface IIdentityResolver
{
    Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default);
}
