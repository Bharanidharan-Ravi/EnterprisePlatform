namespace APIPlatform.Database.Migration.Schema.Models;

/// <summary>
/// One caller-supplied column in a <see cref="TableDefinition"/>. Deliberately a small, closed
/// vocabulary rather than raw SQL: <see cref="Type"/> names a logical type that
/// <see cref="Sql.ColumnTypeMapper"/> maps to the configured provider's real column type, so the
/// same request body creates the same logical table on SQL Server and SAP HANA alike, and so no
/// caller-supplied text ever reaches the database as a type expression.
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>Column name. Must be a plain SQL identifier — see
    /// <see cref="Sql.SchemaIdentifier"/>; anything else is rejected before any SQL is built.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Logical type name, case-insensitive: <c>string</c>, <c>text</c>, <c>int</c>,
    /// <c>long</c>, <c>bool</c>, <c>datetime</c>, <c>decimal</c>, <c>guid</c>, or <c>json</c>.
    /// Defaults to <c>string</c>.</summary>
    public string Type { get; set; } = "string";

    /// <summary>Character length for <c>string</c> columns (1–4000). Ignored for every other
    /// type. Defaults to 200 when omitted.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Whether the column accepts NULL. Defaults to true — a nullable column is the
    /// safe default, and is required outright when adding a column to a table that already has
    /// rows (see <see cref="Abstractions.ISchemaMigrationService.UpdateTableAsync"/>).</summary>
    public bool Nullable { get; set; } = true;

    /// <summary>Marks this column as the table's primary key. At most one field per table may
    /// set it; when no field does, the engine supplies its own <c>Id</c> key column.</summary>
    public bool PrimaryKey { get; set; }

    /// <summary>Creates a unique index on this column.</summary>
    public bool Unique { get; set; }

    /// <summary>Creates a plain (non-unique) index on this column. Ignored when
    /// <see cref="Unique"/> is set, which already builds an index.</summary>
    public bool Indexed { get; set; }
}
