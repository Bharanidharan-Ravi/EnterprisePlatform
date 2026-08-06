using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Playground.Resolvers;

/// <summary>
/// Resolves identities for the playground environment.
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
    /// Resolves the identity for a user.
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

        return Task.FromResult<UserInfo?>(null);
    }
}
