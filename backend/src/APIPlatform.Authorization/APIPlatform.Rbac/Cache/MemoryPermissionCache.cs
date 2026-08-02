using System.Collections.Concurrent;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace APIPlatform.Rbac.Cache;

/// <summary>
/// Default IPermissionCache backed by IMemoryCache. Invalidation is O(1): each (tenant,user)
/// pair has a version counter; InvalidateAsync bumps it rather than removing/walking cache
/// entries, so a role/grant change never requires a distributed cache scan. Swappable for an
/// IDistributedCache-backed implementation in multi-instance deployments (provider independent
/// per Hard Rule 3) — register a different IPermissionCache implementation before AddRbac().
/// </summary>
public sealed class MemoryPermissionCache : IPermissionCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, int> _versions = new();

    public MemoryPermissionCache(IMemoryCache cache) => _cache = cache;

    private static string VersionScope(string tenantId, string userId) => $"{tenantId}:{userId}";

    private static string DataKey(string tenantId, string userId, int version) =>
        $"rbac:permset:{tenantId}:{userId}:v{version}";

    public Task<PermissionSet?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var version = _versions.GetOrAdd(VersionScope(tenantId, userId), 0);
        _cache.TryGetValue(DataKey(tenantId, userId, version), out PermissionSet? set);
        return Task.FromResult(set);
    }

    public Task SetAsync(string tenantId, string userId, PermissionSet permissionSet, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var version = _versions.GetOrAdd(VersionScope(tenantId, userId), 0);
        _cache.Set(DataKey(tenantId, userId, version), permissionSet, ttl ?? TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        _versions.AddOrUpdate(VersionScope(tenantId, userId), 1, (_, v) => v + 1);
        return Task.CompletedTask;
    }
}
