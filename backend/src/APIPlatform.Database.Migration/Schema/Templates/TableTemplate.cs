using APIPlatform.Database.Migration.Schema.Models;

namespace APIPlatform.Database.Migration.Schema.Templates;

/// <summary>
/// A predefined table every line-of-business app tends to need — the shape a caller gets by
/// naming it, instead of restating its columns in every request. A template is nothing more than
/// a name plus a field list in the same <see cref="FieldDefinition"/> vocabulary a caller would
/// have typed, which is what lets a template table and a brand-new table go down one code path.
/// </summary>
public sealed class TableTemplate
{
    /// <summary>The name callers use in <see cref="TableDefinition.Table"/>, matched
    /// case-insensitively (<c>login</c>).</summary>
    public required string Key { get; init; }

    /// <summary>The physical table name created in the database (<c>Logins</c>).</summary>
    public required string TableName { get; init; }

    /// <summary>What this table is for, surfaced by the catalog listing endpoint.</summary>
    public required string Description { get; init; }

    /// <summary>The template's own columns, in order, before the caller's extra fields and the
    /// engine's key/audit columns are applied.</summary>
    public required IReadOnlyList<FieldDefinition> Fields { get; init; }
}
