using System.Security.Cryptography;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Jwt;
using Microsoft.Extensions.Options;

namespace APIPlatform.Authentication.Services;

/// <summary>Default IRefreshTokenService — cryptographically secure opaque tokens stored in
/// IRefreshTokenStore. Replace the store for DB/Redis backing.</summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenStore _store;
    private readonly JwtOptions _options;

    public RefreshTokenService(IRefreshTokenStore store, IOptions<JwtOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public (string Token, DateTimeOffset Expiry) Generate(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
                          .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var expiry = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
        _store.Save(token, userId, expiry);
        return (token, expiry);
    }

    public Task<bool> ValidateAsync(string token, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGet(token, out var entry) && entry.UserId == userId && entry.Expiry > DateTimeOffset.UtcNow);

    public Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        _store.Delete(token);
        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        _store.DeleteAllForUser(userId);
        return Task.CompletedTask;
    }
}

/// <summary>ASSUMPTION BOUNDARY: same pattern as InMemorySessionStore. Replace with a DB/Redis
/// backed implementation by registering a different IRefreshTokenStore.</summary>
public interface IRefreshTokenStore
{
    void Save(string token, string userId, DateTimeOffset expiry);
    bool TryGet(string token, out RefreshTokenEntry entry);
    void Delete(string token);
    void DeleteAllForUser(string userId);
}

public sealed record RefreshTokenEntry(string UserId, DateTimeOffset Expiry);

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, RefreshTokenEntry> _store = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    public void Save(string token, string userId, DateTimeOffset expiry)
    {
        lock (_lock) _store[token] = new(userId, expiry);
    }

    public bool TryGet(string token, out RefreshTokenEntry entry)
    {
        lock (_lock) return _store.TryGetValue(token, out entry!);
    }

    public void Delete(string token) { lock (_lock) _store.Remove(token); }

    public void DeleteAllForUser(string userId)
    {
        lock (_lock)
        {
            var keys = _store.Where(kv => kv.Value.UserId == userId).Select(kv => kv.Key).ToList();
            foreach (var k in keys) _store.Remove(k);
        }
    }
}
