using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Playground.Resolvers;

/// <summary>
/// TEST ONLY — resolves identities for the Playground environment against two hardcoded logins.
/// There is no real user store behind this; it exists purely so the Playground host has
/// something to authenticate against for manual/Phase-2 testing (login, RBAC allow/deny proof).
/// The platform must never depend on either of these hardcoded users — a real deployment
/// supplies its own <see cref="IIdentityResolver"/> backed by a real user store.
/// </summary>
public class PlaygroundIdentityResolver : IIdentityResolver
{
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaygroundIdentityResolver"/> class.
    /// </summary>
    public PlaygroundIdentityResolver(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Resolves the identity for a user. TEST ONLY: "admin" (full Employee CRUD, seeded via
    /// EmployeeModuleInitializationService) and "viewer" (Employee read-only) are the only two
    /// recognized logins.
    /// </summary>
    public Task<UserInfo?> ResolveAsync(string loginIdentifier, string? tenantId, CancellationToken cancellationToken = default)
    {
        if (loginIdentifier == "admin")
        {
            return Task.FromResult<UserInfo?>(new UserInfo
            {
                UserId = "user-123",
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = _passwordHasher.Hash("Admin@123"),
                IsActive = true,
                IsLocked = false
            });
        }

        if (loginIdentifier == "viewer")
        {
            return Task.FromResult<UserInfo?>(new UserInfo
            {
                UserId = "user-456",
                Username = "viewer",
                Email = "viewer@example.com",
                PasswordHash = _passwordHasher.Hash("Viewer@123"),
                IsActive = true,
                IsLocked = false
            });
        }

        return Task.FromResult<UserInfo?>(null);
    }

    /// <summary>TEST ONLY, same two hardcoded logins as ResolveAsync — looked up by Id instead of
    /// username for refresh-token rotation.</summary>
    public Task<UserInfo?> ResolveByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (userId == "user-123") return ResolveAsync("admin", null, cancellationToken);
        if (userId == "user-456") return ResolveAsync("viewer", null, cancellationToken);
        return Task.FromResult<UserInfo?>(null);
    }
}
