using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Defaults;

/// <summary>Safe default so AddCrudEngine() works out of the box with no defaults configured.
/// Apps that want default-value behavior register their own IEntityDefaultValueProvider, which
/// takes precedence — see ServiceCollectionExtensions.</summary>
public sealed class NoOpEntityDefaultValueProvider : IEntityDefaultValueProvider
{
    public EntityDefaultValueConfig? TryGetConfig(string entityName) => null;
}
