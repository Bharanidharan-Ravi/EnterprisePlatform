namespace APIPlatform.Rbac.Contexts;

/// <summary>
/// A provider-agnostic description of a row-level filter. Rbac never builds an
/// Expression&lt;Func&lt;T,bool&gt;&gt; or a SQL fragment itself (that would create a hard
/// dependency on Dapper/EF/Data, violating the "minimal dependency" rule) — it hands back a
/// named filter + parameters, and APIPlatform.Data / CrudEngine is responsible for turning
/// FilterName into an actual query predicate for whatever provider is in use.
/// </summary>
public sealed class RowFilterDescriptor
{
    public required string FilterName { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();

    public static RowFilterDescriptor None { get; } = new() { FilterName = "None" };
}
