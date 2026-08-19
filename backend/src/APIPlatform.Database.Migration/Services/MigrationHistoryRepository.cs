using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Models;
using APIPlatform.Database.Migration.Sql;
using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Services;

/// <summary>Dapper/IDatabaseExecutor-backed <see cref="IMigrationHistoryRepository"/>. All SQL
/// text comes from <see cref="MigrationHistorySqlBuilder"/>; this class only binds parameters
/// and maps rows — same split NotificationRepository uses.</summary>
public sealed class MigrationHistoryRepository : IMigrationHistoryRepository
{
    private readonly IDatabaseExecutor _executor;
    private readonly IMigrationSqlDialectResolver _dialectResolver;

    public MigrationHistoryRepository(IDatabaseExecutor executor, IMigrationSqlDialectResolver dialectResolver)
    {
        _executor = executor;
        _dialectResolver = dialectResolver;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (await TableExistsAsync(cancellationToken)) return;

        var dialect = _dialectResolver.Resolve();
        try
        {
            await _executor.ExecuteAsync(MigrationHistorySqlBuilder.CreateHistoryTable(dialect), cancellationToken: cancellationToken);
        }
        catch (DatabaseException)
        {
            // Race: another runner created the table between our existence check and this CREATE.
            // Only swallow it if the table genuinely exists now — a real DDL failure (permissions,
            // connectivity) must still surface, not be mistaken for a benign race.
            if (!await TableExistsAsync(cancellationToken)) throw;
        }
    }

    public async Task<IReadOnlySet<string>> GetAppliedMigrationIdsAsync(CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = MigrationHistorySqlBuilder.SelectAppliedMigrationIds(dialect);
        var ids = await _executor.QueryAsync<string>(sql, cancellationToken: cancellationToken);
        return ids.ToHashSet();
    }

    public async Task RecordAppliedAsync(AppliedMigration migration, IDatabaseTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = MigrationHistorySqlBuilder.InsertAppliedMigration(dialect);
        var parameters = new Dictionary<string, object?>
        {
            ["Id"] = migration.Id,
            ["MigrationId"] = migration.MigrationId,
            ["Version"] = migration.Version,
            ["Description"] = migration.Description,
            ["AppliedOnUtc"] = migration.AppliedOnUtc.UtcDateTime
        };

        try
        {
            await _executor.ExecuteAsync(sql, parameters, transaction: transaction, cancellationToken: cancellationToken);
        }
        catch (DatabaseException)
        {
            // Race: another runner already recorded this exact MigrationId first (unique
            // constraint violation) — that means the migration is now genuinely applied, the
            // outcome we wanted, so only rethrow if the row still isn't actually there.
            var applied = await GetAppliedMigrationIdsAsync(cancellationToken);
            if (!applied.Contains(migration.MigrationId)) throw;
        }
    }

    private async Task<bool> TableExistsAsync(CancellationToken cancellationToken) =>
        await _executor.ExecuteScalarAsync<int>(MigrationHistorySqlBuilder.TableExists(), cancellationToken: cancellationToken) > 0;
}
