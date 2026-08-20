using APIPlatform.CrudEngine.Interfaces;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Tests.TestSupport;

internal sealed class FakeEntityDefinitionProvider : IEntityDefinitionProvider
{
    private readonly Func<string, EntityDefinition> _resolve;

    public FakeEntityDefinitionProvider(Func<string, EntityDefinition> resolve) => _resolve = resolve;

    public EntityDefinition GetDefinition(string entityName) => _resolve(entityName);
}
