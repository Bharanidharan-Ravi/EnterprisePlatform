using APIPlatform.Database.Migration.Schema.Models;

namespace APIPlatform.Database.Migration.Schema.Abstractions;

/// <summary>
/// Runtime, config-driven schema management: create, update, and delete tables from a request
/// body rather than from migration classes fixed at build time.
///
/// <para>This is deliberately a different mechanism from <see cref="Migration.Abstractions.IMigration"/>
/// and <see cref="Migration.Abstractions.IMigrationRunner"/>, not a replacement for it. The versioned runner
/// exists for schema changes that ship <i>with</i> a release and must be applied exactly once, in
/// order, on every environment — it tracks what it has applied in <c>MigrationHistory</c>. This
/// service exists for schema an operator or app defines at run time, where "has this been applied"
/// is not a meaningful question because there is no fixed set to apply; it therefore keeps no
/// history and instead reads the live catalog (INFORMATION_SCHEMA) to decide what to do.</para>
///
/// <para>Because every operation here builds DDL from caller input, treat these endpoints as
/// privileged: they are schema administration, and should sit behind the same authorization a
/// database administrator would need.</para>
/// </summary>
public interface ISchemaMigrationService
{
    /// <summary>
    /// Creates a table from <paramref name="definition"/> — a predefined template's columns when
    /// the name matches one (plus any extra fields supplied), otherwise a new table built entirely
    /// from the supplied fields. Returns <see cref="SchemaOperationStatus.Conflict"/> without
    /// executing anything if the table already exists; existing tables are never altered or
    /// dropped by this method.
    /// </summary>
    Task<SchemaOperationResult> CreateTableAsync(TableDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds any columns in <paramref name="definition"/> that the existing table does not have,
    /// leaving every existing column untouched. Additive only: nothing is retyped, renamed, or
    /// dropped, since those operations lose data and cannot be expressed safely from a request
    /// body. New columns are always created nullable regardless of the request, because a NOT NULL
    /// column cannot be added to a table that already has rows.
    /// </summary>
    Task<SchemaOperationResult> UpdateTableAsync(TableDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops <paramref name="tableName"/> and every row in it. Irreversible — callers are expected
    /// to require explicit confirmation before invoking this.
    /// </summary>
    Task<SchemaOperationResult> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>Whether <paramref name="tableName"/> currently exists in the database.</summary>
    Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default);
}
