namespace APIPlatform.Database.Migration.Schema.Models;

/// <summary>
/// The request body for every schema operation. <see cref="Template"/> and <see cref="Table"/>
/// are deliberately separate concerns:
///
/// <para><see cref="Template"/> selects a predefined column set from
/// <see cref="Templates.TableTemplateCatalog"/> (<c>login</c>, <c>audit</c>, <c>notification</c>,
/// …) — it is never itself a table name. Any <see cref="Fields"/> the caller also supplies are
/// appended as extra columns on top of the template's, so an app that needs two more fields on
/// its login table sends them in the same body instead of needing new platform code.</para>
///
/// <para><see cref="Table"/> is the physical table name the operation targets. When a
/// <see cref="Template"/> is given and <see cref="Table"/> is left empty, the template's own
/// table name is used (<c>login</c> → <c>Logins</c>); supplying <see cref="Table"/> creates that
/// template's columns under a name of the caller's choosing instead — e.g. a second, differently
/// named login table for another tenant or subsystem. When no <see cref="Template"/> is given,
/// <see cref="Table"/> is required and the table is built entirely from <see cref="Fields"/>.</para>
///
/// Either way the engine supplies the key column and the standard audit columns, so every table
/// it creates has the same spine regardless of which path produced it.
/// </summary>
public sealed class TableDefinition
{
    /// <summary>Predefined column set to use (<c>login</c>, <c>audit</c>, …), matched against
    /// <see cref="Templates.TableTemplateCatalog"/> by key, case-insensitively. Leave empty to
    /// build a table entirely from <see cref="Fields"/>.</summary>
    public string? Template { get; set; }

    /// <summary>Physical table name to create/alter/drop. Required unless <see cref="Template"/>
    /// is set, in which case an empty value falls back to that template's own table name. Must be
    /// a plain SQL identifier.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Extra columns for a template table, or the complete column list for a new one.</summary>
    public List<FieldDefinition> Fields { get; set; } = [];

    /// <summary>Whether to append the standard audit columns (CreatedBy, CreatedOnUtc,
    /// LastModifiedBy, LastModifiedOnUtc). Defaults to true.</summary>
    public bool IncludeAudit { get; set; } = true;

    /// <summary>Whether to append an <c>AdditionalData</c> JSON column — the place per-record
    /// caller-specific values live when they don't warrant their own column. Defaults to true.</summary>
    public bool IncludeAdditionalData { get; set; } = true;
}
