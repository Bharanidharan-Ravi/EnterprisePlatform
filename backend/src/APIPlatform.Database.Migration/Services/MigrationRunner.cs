using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Models;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Foundation.Interfaces;
using Microsoft.Extensions.Options;

namespace APIPlatform.Database.Migration.Services;

/// <summary>
/// Default <see cref="IMigrationRunner"/>. Filters every registered <see cref="IMigration"/> down
/// to the ones matching the configured <c>DatabaseOptions.Provider</c> that are not yet in the
/// migration history, applies them in ascending Version order, and records each one as it
/// succeeds. Stops at the first failure — an inconsistent-state schema is never silently left
/// half-migrated with the runner claiming success.
/// </summary>
public sealed class MigrationRunner : IMigrationRunner
{
    private readonly IReadOnlyList<IMigration> _migrations;
    private readonly IMigrationHistoryRepository _history;
    private readonly IMigrationSqlDialectResolver _dialectResolver;
    private readonly IDatabaseExecutor _executor;
    private readonly DatabaseOptions _options;
    private readonly IClock _clock;

    public MigrationRunner(
        IEnumerable<IMigration> migrations,
        IMigrationHistoryRepository history,
        IMigrationSqlDialectResolver dialectResolver,
        IDatabaseExecutor executor,
        IOptions<DatabaseOptions> options,
        IClock clock)
    {
        _migrations = migrations.ToList();
        _history = history;
        _dialectResolver = dialectResolver;
        _executor = executor;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<MigrationRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        await _history.EnsureCreatedAsync(cancellationToken);
        var appliedIds = await _history.GetAppliedMigrationIdsAsync(cancellationToken);

        var forActiveProvider = _migrations.Where(m => m.SupportedProvider == _options.Provider).ToList();

        var pending = forActiveProvider
            .Where(m => !appliedIds.Contains(m.MigrationId))
            .OrderBy(m => m.Version)
            .ThenBy(m => m.MigrationId, StringComparer.Ordinal)
            .ToList();

        var skipped = forActiveProvider
            .Where(m => appliedIds.Contains(m.MigrationId))
            .Select(m => m.MigrationId)
            .Distinct()
            .ToList();

        var applied = new List<AppliedMigration>();
        var dialect = _dialectResolver.Resolve();

        foreach (var migration in pending)
        {
            var record = new AppliedMigration
            {
                Id = Guid.NewGuid().ToString(),
                MigrationId = migration.MigrationId,
                Version = migration.Version,
                Description = migration.Description,
                AppliedOnUtc = _clock.UtcNow
            };

            try
            {
                if (dialect.SupportsTransactionalDdl)
                {
                    await using var transaction = await _executor.BeginTransactionAsync(cancellationToken: cancellationToken);
                    await migration.ApplyAsync(_executor, transaction, cancellationToken);
                    await _history.RecordAppliedAsync(record, transaction, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    // SAP HANA: DDL auto-commits regardless of any transaction we open, so
                    // wrapping it here would only imply a rollback safety net this engine can't
                    // provide — run the migration directly, then record it, both auto-committing.
                    await migration.ApplyAsync(_executor, transaction: null, cancellationToken);
                    await _history.RecordAppliedAsync(record, transaction: null, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MigrationException(
                    $"Migration '{migration.MigrationId}' (v{migration.Version}, {migration.SupportedProvider}) failed to apply.", ex)
                {
                    FailedMigrationId = migration.MigrationId,
                    AppliedBeforeFailure = applied
                };
            }

            applied.Add(record);
        }

        return new MigrationRunResult { Applied = applied, Skipped = skipped };
    }
}
