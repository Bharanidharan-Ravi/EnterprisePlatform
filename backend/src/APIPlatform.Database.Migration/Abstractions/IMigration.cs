using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;

namespace APIPlatform.Database.Migration.Abstractions;

/// <summary>
/// One versioned unit of database schema deployment. A migration knows how to apply itself for
/// exactly one <see cref="DatabaseProvider"/> — a logical migration that must exist on both SQL
/// Server and SAP HANA is expressed as two <see cref="IMigration"/> instances sharing the same
/// <see cref="MigrationId"/>/<see cref="Version"/> (one per provider), not as one implementation
/// that branches internally. <see cref="IMigrationRunner"/> selects only the instance matching
/// the application's configured provider.
/// </summary>
public interface IMigration
{
    /// <summary>Stable, globally unique identifier for this logical migration — shared across the
    /// SQL Server and SAP HANA variants of "the same" migration, since <see cref="Services.MigrationHistoryRepository"/>
    /// tracks history per logical migration, not per provider variant. Never reused once shipped.</summary>
    string MigrationId { get; }

    /// <summary>Ordering key within the migration history. Migrations run in ascending
    /// <see cref="Version"/> order regardless of registration order.</summary>
    int Version { get; }

    /// <summary>Short human-readable description, recorded in the migration history for auditing.</summary>
    string Description { get; }

    /// <summary>The single provider this migration instance applies to.</summary>
    DatabaseProvider SupportedProvider { get; }

    /// <summary>
    /// Applies this migration's schema changes. When <paramref name="transaction"/> is non-null
    /// the runner has already begun a transaction the migration must execute within (SQL Server,
    /// where DDL is transactional); when null, the target provider does not support transactional
    /// DDL (SAP HANA — every statement auto-commits) and each statement should be issued directly
    /// against <paramref name="executor"/>, one at a time.
    /// </summary>
    Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default);
}
