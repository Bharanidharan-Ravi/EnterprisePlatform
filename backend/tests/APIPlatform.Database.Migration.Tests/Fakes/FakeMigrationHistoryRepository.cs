using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Models;

namespace APIPlatform.Database.Migration.Tests.Fakes;

/// <summary>In-memory IMigrationHistoryRepository test double, so MigrationRunnerTests can focus
/// purely on the runner's discovery/ordering/failure-handling logic without also asserting
/// MigrationHistoryRepository's SQL (that's MigrationHistoryRepositoryTests's job).</summary>
internal sealed class FakeMigrationHistoryRepository : IMigrationHistoryRepository
{
    public HashSet<string> AppliedIds { get; } = [];
    public List<AppliedMigration> Recorded { get; } = [];
    public bool EnsureCreatedCalled { get; private set; }
    public Func<AppliedMigration, Exception?> OnRecordApplied { get; set; } = _ => null;

    public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        EnsureCreatedCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlySet<string>> GetAppliedMigrationIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<string>>(AppliedIds);

    public Task RecordAppliedAsync(AppliedMigration migration, IDatabaseTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var failure = OnRecordApplied(migration);
        if (failure is not null) throw failure;

        Recorded.Add(migration);
        AppliedIds.Add(migration.MigrationId);
        return Task.CompletedTask;
    }
}
