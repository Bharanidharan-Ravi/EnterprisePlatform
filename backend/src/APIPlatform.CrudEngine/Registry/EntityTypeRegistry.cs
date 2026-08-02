using APIPlatform.CrudEngine.Interfaces;

namespace APIPlatform.CrudEngine.Registry;

/// <summary>Default in-memory IEntityTypeRegistry, populated once at construction from every
/// registered IEntityTypeSeed (one per AddEntityType&lt;T&gt;() call). Thread-safe for read-only
/// access after DI container build.</summary>
public sealed class EntityTypeRegistry : IEntityTypeRegistry
{
    private readonly Dictionary<string, Type> _map;

    public EntityTypeRegistry(IEnumerable<IEntityTypeSeed> seeds) =>
        _map = seeds.ToDictionary(s => s.EntityName, s => s.ClrType, StringComparer.OrdinalIgnoreCase);

    public Type Resolve(string entityName) =>
        TryResolve(entityName, out var type)
            ? type
            : throw new InvalidOperationException(
                $"No CLR type registered for entity '{entityName}'. Call AddEntityType<T>(\"{entityName}\") at startup.");

    public bool TryResolve(string entityName, out Type type) => _map.TryGetValue(entityName, out type!);
}

/// <summary>Default IEntityTypeSeed record.</summary>
public sealed record EntityTypeSeed(string EntityName, Type ClrType) : IEntityTypeSeed;
