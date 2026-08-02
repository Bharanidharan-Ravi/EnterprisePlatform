using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Interfaces;

/// <summary>
/// Supplies stored-procedure operation bindings per entity. Nucleus never hardcodes these —
/// the consuming/generated app registers an implementation backed by whatever config source
/// it uses (JSON, DB table, appsettings). Config is the source of truth (Rule 2).
/// </summary>
public interface IEntityOperationBindingProvider
{
    EntityOperationBinding? TryGetBinding(string entityName);
}

/// <summary>Supplies multi-result-set operation configs by operation key. Same rationale as
/// <see cref="IEntityOperationBindingProvider"/> — consuming app owns the config, not Nucleus.</summary>
public interface IMultiResultOperationProvider
{
    MultiResultOperationConfig? TryGetConfig(string operationKey);
}

/// <summary>
/// Maps an entity name (as it appears in EntityDefinition.Name / config) to its CLR type at
/// runtime. Required because batch and multi-result execution work off entity-name strings,
/// while GenericRepository&lt;TEntity&gt; and Dapper mapping need a concrete Type. Populated via
/// AddEntityType&lt;T&gt;() during startup — one line per generated entity, no reflection scanning
/// magic that would be hard to reason about across a 10+ year codebase.
/// </summary>
public interface IEntityTypeRegistry
{
    Type Resolve(string entityName);
    bool TryResolve(string entityName, out Type type);
}

/// <summary>DI seed record consumed by EntityTypeRegistry's constructor — one instance per
/// AddEntityType&lt;T&gt;() call, collected via IEnumerable&lt;IEntityTypeSeed&gt; so registration
/// order/timing never matters.</summary>
public interface IEntityTypeSeed
{
    string EntityName { get; }
    Type ClrType { get; }
}
