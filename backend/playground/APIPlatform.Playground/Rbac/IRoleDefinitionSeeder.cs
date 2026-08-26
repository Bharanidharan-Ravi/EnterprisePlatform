using APIPlatform.Rbac.Models;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Optional capability a durable IRoleStore implementation can expose for defining a Role's
/// existence (Id/Name/TenantId/ParentRoleId/IsSystemRole). APIPlatform.Rbac's own IRoleStore
/// interface has no such method — InMemoryRoleStore exposes an equivalent only as a synchronous,
/// concrete-type-only SeedRole(Role), which doesn't fit an async, I/O-backed durable store.
/// Idempotent: calling this again for a role that already exists must not fail or duplicate it.
/// </summary>
public interface IRoleDefinitionSeeder
{
    Task EnsureRoleAsync(Role role, CancellationToken cancellationToken = default);
}
