using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;
using APIPlatform.Database.Migration.Models;
using APIPlatform.Database.Migration.Services;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Database.Migration.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Services;

/// <summary>
/// MigrationRunner's discovery/provider-filtering/ordering/idempotency/failure-handling and
/// transactional-DDL-per-dialect behavior, against FakeDatabaseExecutor + FakeMigrationHistoryRepository.
/// No live database is needed for any of this.
/// </summary>
public class MigrationRunnerTests
{
    private sealed class StaticDialectResolver(IMigrationSqlDialect dialect) : IMigrationSqlDialectResolver
    {
        public IMigrationSqlDialect Resolve() => dialect;
    }

    private static MigrationRunner CreateRunner(
        IEnumerable<IMigration> migrations,
        FakeMigrationHistoryRepository history,
        FakeDatabaseExecutor executor,
        IMigrationSqlDialect? dialect = null,
        DatabaseProvider provider = DatabaseProvider.SqlServer,
        FakeClock? clock = null) =>
        new(
            migrations,
            history,
            new StaticDialectResolver(dialect ?? new SqlServerMigrationDialect()),
            executor,
            Options.Create(new DatabaseOptions { ConnectionString = "unused", Provider = provider }),
            clock ?? new FakeClock());

    [Fact]
    public async Task RunAsync_FiltersToMigrationsMatchingActiveProvider()
    {
        var sqlServerMigration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var hanaMigration = new FakeMigration { MigrationId = "B", Version = 1, SupportedProvider = DatabaseProvider.Hana };
        var history = new FakeMigrationHistoryRepository();

        var result = await CreateRunner([sqlServerMigration, hanaMigration], history, new FakeDatabaseExecutor(), provider: DatabaseProvider.SqlServer)
            .RunAsync();

        Assert.True(sqlServerMigration.Applied);
        Assert.False(hanaMigration.Applied);
        Assert.Single(result.Applied);
        Assert.Equal("A", result.Applied[0].MigrationId);
    }

    [Fact]
    public async Task RunAsync_AppliesInAscendingVersionOrder_RegardlessOfRegistrationOrder()
    {
        var order = new List<string>();
        var v3 = new RecordingMigration("C", 3, order);
        var v1 = new RecordingMigration("A", 1, order);
        var v2 = new RecordingMigration("B", 2, order);
        var history = new FakeMigrationHistoryRepository();

        await CreateRunner([v3, v1, v2], history, new FakeDatabaseExecutor()).RunAsync();

        Assert.Equal(["A", "B", "C"], order);
    }

    [Fact]
    public async Task RunAsync_AlreadyApplied_IsSkippedNotReapplied()
    {
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();
        history.AppliedIds.Add("A");

        var result = await CreateRunner([migration], history, new FakeDatabaseExecutor()).RunAsync();

        Assert.False(migration.Applied);
        Assert.Empty(result.Applied);
        Assert.Contains("A", result.Skipped);
    }

    [Fact]
    public async Task RunAsync_CalledTwice_SecondRunIsANoOp_Idempotent()
    {
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();
        var runner = CreateRunner([migration], history, new FakeDatabaseExecutor());

        var first = await runner.RunAsync();
        var second = await runner.RunAsync();

        Assert.Single(first.Applied);
        Assert.Empty(second.Applied);
        Assert.Contains("A", second.Skipped);
    }

    [Fact]
    public async Task RunAsync_EnsuresHistoryTableBeforeApplyingAnything()
    {
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();

        await CreateRunner([migration], history, new FakeDatabaseExecutor()).RunAsync();

        Assert.True(history.EnsureCreatedCalled);
    }

    [Fact]
    public async Task RunAsync_MidRunFailure_StopsAndReportsWhatSucceededBeforeIt()
    {
        var first = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var second = new FakeMigration { MigrationId = "B", Version = 2, SupportedProvider = DatabaseProvider.SqlServer, FailWith = new InvalidOperationException("bad DDL") };
        var third = new FakeMigration { MigrationId = "C", Version = 3, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();
        var runner = CreateRunner([first, second, third], history, new FakeDatabaseExecutor());

        var ex = await Assert.ThrowsAsync<MigrationException>(() => runner.RunAsync());

        Assert.Equal("B", ex.FailedMigrationId);
        Assert.Single(ex.AppliedBeforeFailure);
        Assert.Equal("A", ex.AppliedBeforeFailure[0].MigrationId);
        Assert.True(first.Applied);
        Assert.False(second.Applied);
        Assert.False(third.Applied); // never attempted — fail-fast
    }

    [Fact]
    public async Task RunAsync_SqlServer_WrapsMigrationAndHistoryInsertInOneTransaction_AndCommits()
    {
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();
        FakeDatabaseTransaction? transaction = null;
        var executor = new FakeDatabaseExecutor();
        executor.OnBeginTransaction = () => transaction = new FakeDatabaseTransaction();

        await CreateRunner([migration], history, executor, dialect: new SqlServerMigrationDialect()).RunAsync();

        Assert.Equal(1, executor.BeginTransactionCallCount);
        Assert.True(migration.ReceivedTransaction);
        Assert.True(transaction!.Committed);
    }

    [Fact]
    public async Task RunAsync_Hana_DoesNotOpenATransaction_BecauseDdlAutoCommitsRegardless()
    {
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.Hana };
        var history = new FakeMigrationHistoryRepository();
        var executor = new FakeDatabaseExecutor();

        await CreateRunner([migration], history, executor, dialect: new HanaMigrationDialect(), provider: DatabaseProvider.Hana).RunAsync();

        Assert.Equal(0, executor.BeginTransactionCallCount);
        Assert.False(migration.ReceivedTransaction);
    }

    [Fact]
    public async Task RunAsync_UsesClockForAppliedOnUtc()
    {
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero) };
        var migration = new FakeMigration { MigrationId = "A", Version = 1, SupportedProvider = DatabaseProvider.SqlServer };
        var history = new FakeMigrationHistoryRepository();

        var result = await CreateRunner([migration], history, new FakeDatabaseExecutor(), clock: clock).RunAsync();

        Assert.Equal(clock.UtcNow, result.Applied[0].AppliedOnUtc);
    }

    /// <summary>Records the order ApplyAsync was actually invoked in, independent of registration order.</summary>
    private sealed class RecordingMigration(string id, int version, List<string> order) : IMigration
    {
        public string MigrationId => id;
        public int Version => version;
        public string Description => "recording";
        public DatabaseProvider SupportedProvider => DatabaseProvider.SqlServer;

        public Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
        {
            order.Add(id);
            return Task.CompletedTask;
        }
    }
}
