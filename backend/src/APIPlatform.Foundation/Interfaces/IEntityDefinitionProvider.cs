using Nucleus.SharedSchema.Models;

namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// Resolves <see cref="EntityDefinition"/>s without exposing where they come from
/// (JSON, database, Builder, memory, or a remote API).
/// </summary>
public interface IEntityDefinitionProvider
{
    /// <summary>Resolves a single entity definition by its technical name.</summary>
    EntityDefinition GetDefinition(string entityName);

    /// <summary>Resolves every entity definition known to the current provider.</summary>
    IReadOnlyCollection<EntityDefinition> GetDefinitions();
}
