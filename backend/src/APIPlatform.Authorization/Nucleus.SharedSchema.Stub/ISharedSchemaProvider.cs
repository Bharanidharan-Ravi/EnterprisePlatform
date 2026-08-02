namespace Nucleus.SharedSchema;

/// <summary>
/// STUB — placeholder for the real Nucleus.SharedSchema package. Rbac takes this as an
/// OPTIONAL dependency (nullable in DI) so it still functions if Shared Schema isn't wired
/// up yet — field-level default-permission lookups simply no-op until it is.
/// </summary>
public interface ISharedSchemaProvider
{
    Task<EntityMetadata?> GetEntityMetadataAsync(string entityKey, CancellationToken cancellationToken = default);
}
