using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Caching;

/// <summary>Resolves EntityDefinition once and caches it (Req 12) instead of every pipeline run
/// re-hitting IEntityDefinitionProvider. Cache is process-lifetime; definitions are treated as
/// immutable per app run, consistent with SharedSchema being config loaded at startup.</summary>
public interface IEntityMetadataCache
{
    EntityDefinition GetDefinition(string entityName);
}
