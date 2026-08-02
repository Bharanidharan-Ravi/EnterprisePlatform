using System.Collections.Concurrent;
using APIPlatform.Foundation.Interfaces;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Caching;

/// <summary>Default IEntityMetadataCache — thread-safe lazy cache over IEntityDefinitionProvider.</summary>
public sealed class EntityMetadataCache : IEntityMetadataCache
{
    private readonly IEntityDefinitionProvider _provider;
    private readonly ConcurrentDictionary<string, EntityDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);

    public EntityMetadataCache(IEntityDefinitionProvider provider) => _provider = provider;

    public EntityDefinition GetDefinition(string entityName) =>
        _cache.GetOrAdd(entityName, _provider.GetDefinition);
}
