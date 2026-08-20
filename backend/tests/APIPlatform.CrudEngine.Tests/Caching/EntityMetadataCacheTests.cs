using APIPlatform.CrudEngine.Caching;
using APIPlatform.CrudEngine.Tests.TestSupport;
using Xunit;

namespace APIPlatform.CrudEngine.Tests.Caching;

/// <summary>Covers metadata resolution (Phase 1 Section 7): EntityMetadataCache resolves once per
/// entity name via IEntityDefinitionProvider and reuses the same instance thereafter.</summary>
public class EntityMetadataCacheTests
{
    [Fact]
    public void GetDefinition_CalledTwiceForSameEntity_OnlyHitsProviderOnce()
    {
        var callCount = 0;
        var widget = EntityDefinitions.Widget();
        var provider = new FakeEntityDefinitionProvider(_ => { callCount++; return widget; });
        var cache = new EntityMetadataCache(provider);

        var first = cache.GetDefinition("Widget");
        var second = cache.GetDefinition("Widget");

        Assert.Same(first, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void GetDefinition_IsCaseInsensitiveByEntityName()
    {
        var widget = EntityDefinitions.Widget();
        var callCount = 0;
        var provider = new FakeEntityDefinitionProvider(_ => { callCount++; return widget; });
        var cache = new EntityMetadataCache(provider);

        cache.GetDefinition("Widget");
        cache.GetDefinition("widget");

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void GetDefinition_DifferentEntityNames_ResolveIndependently()
    {
        var widget = EntityDefinitions.Widget();
        var gadget = EntityDefinitions.Widget() with { Name = "Gadget", SourceName = "Gadgets" };
        var provider = new FakeEntityDefinitionProvider(name => name == "Widget" ? widget : gadget);
        var cache = new EntityMetadataCache(provider);

        Assert.Equal("Widgets", cache.GetDefinition("Widget").SourceName);
        Assert.Equal("Gadgets", cache.GetDefinition("Gadget").SourceName);
    }
}
