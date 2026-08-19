using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Models;

namespace APIPlatform.Database.Migration.Abstractions;

/// <summary>
/// Tracks which migrations have already been applied. Backed by a minimal, provider-agnostic
/// <c>MigrationHistory</c> table that the repository creates on first use — the one piece of
/// schema this package owns for itself, everything else it deploys belongs to the consumer.
/// </summary>
public interface IMigrationHistoryRepository
{
    /// <summary>Creates the <c>MigrationHistory</c> table if it does not already exist. Safe to
    /// call every run and safe under a create/create race between two concurrent runners.</summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>The set of <see cref="IMigration.MigrationId"/> values already recorded as applied.</summary>
    Task<IReadOnlySet<string>> GetAppliedMigrationIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a migration as applied. Pass <paramref name="transaction"/> when the
    /// active dialect supports transactional DDL, so the history row commits atomically with the
    /// migration's own schema changes. Treated as a success (not rethrown) if a concurrent runner
    /// already inserted the same MigrationId first — see <c>MigrationHistoryRepository</c> for
    /// the exact race handling.</summary>
    Task RecordAppliedAsync(AppliedMigration migration, IDatabaseTransaction? transaction = null, CancellationToken cancellationToken = default);
}
