using APIPlatform.Data.Exceptions;
using APIPlatform.Database.Migration.Models;
using APIPlatform.Database.Migration.Services;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Database.Migration.Tests.Fakes;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Services;

/// <summary>
/// Exercises MigrationHistoryRepository's control flow (existence-check-before-create, the
/// create/create race, the insert/insert race) against FakeDatabaseExecutor — whitebox, relying
/// on InternalsVisibleTo, mirroring NotificationRepositoryTests. No live database is needed.
/// </summary>
public class MigrationHistoryRepositoryTests
{
    private sealed class StaticDialectResolver(IMigrationSqlDialect dialect) : IMigrationSqlDialectResolver
    {
        public IMigrationSqlDialect Resolve() => dialect;
    }

    private static MigrationHistoryRepository CreateRepository(FakeDatabaseExecutor executor) =>
        new(executor, new StaticDialectResolver(new SqlServerMigrationDialect()));

    [Fact]
    public async Task EnsureCreatedAsync_TableDoesNotExist_CreatesIt()
    {
        var executor = new FakeDatabaseExecutor { OnExecuteScalar = _ => 0 };
        var repository = CreateRepository(executor);

        await repository.EnsureCreatedAsync();

        Assert.Contains(executor.ExecuteCalls, c => c.Sql.StartsWith("CREATE TABLE [MigrationHistory]"));
    }

    [Fact]
    public async Task EnsureCreatedAsync_TableAlreadyExists_DoesNotCreateIt()
    {
        var executor = new FakeDatabaseExecutor { OnExecuteScalar = _ => 1 };
        var repository = CreateRepository(executor);

        await repository.EnsureCreatedAsync();

        Assert.DoesNotContain(executor.ExecuteCalls, c => c.Sql.StartsWith("CREATE TABLE"));
    }

    [Fact]
    public async Task EnsureCreatedAsync_CreateRacesAndTableNowExists_DoesNotThrow()
    {
        var scalarCallCount = 0;
        var executor = new FakeDatabaseExecutor
        {
            OnExecuteScalar = _ => scalarCallCount++ == 0 ? 0 : 1, // first check: missing; re-check after race: present
            OnExecute = call =>
            {
                if (call.Sql.StartsWith("CREATE TABLE")) throw new DatabaseException("already exists (another runner won the race)");
                return 0;
            }
        };
        var repository = CreateRepository(executor);

        await repository.EnsureCreatedAsync(); // must not throw

        Assert.Equal(2, scalarCallCount);
    }

    [Fact]
    public async Task EnsureCreatedAsync_CreateFailsAndTableStillMissing_Rethrows()
    {
        var executor = new FakeDatabaseExecutor
        {
            OnExecuteScalar = _ => 0, // both the initial check and the re-check report "missing"
            OnExecute = call =>
            {
                if (call.Sql.StartsWith("CREATE TABLE")) throw new DatabaseException("real infrastructure failure");
                return 0;
            }
        };
        var repository = CreateRepository(executor);

        var ex = await Assert.ThrowsAsync<DatabaseException>(() => repository.EnsureCreatedAsync());
        Assert.Equal("real infrastructure failure", ex.Message);
    }

    [Fact]
    public async Task GetAppliedMigrationIdsAsync_ReturnsIdsFromQuery()
    {
        var executor = new FakeDatabaseExecutor { OnQuery = _ => ["Notification.Schema.v1", "Other.Schema.v1"] };
        var repository = CreateRepository(executor);

        var ids = await repository.GetAppliedMigrationIdsAsync();

        Assert.Equal(2, ids.Count);
        Assert.Contains("Notification.Schema.v1", ids);
    }

    [Fact]
    public async Task RecordAppliedAsync_InsertsAllColumns()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 1 };
        var repository = CreateRepository(executor);
        var migration = new AppliedMigration
        {
            Id = "H1", MigrationId = "Notification.Schema.v1", Version = 1,
            Description = "desc", AppliedOnUtc = DateTimeOffset.UtcNow
        };

        await repository.RecordAppliedAsync(migration);

        var call = executor.ExecuteCalls.Single();
        Assert.Equal("Notification.Schema.v1", call.Parameters!["MigrationId"]);
        Assert.Equal(1, call.Parameters["Version"]);
    }

    [Fact]
    public async Task RecordAppliedAsync_InsertRacesAndLoses_ButIdNowPresent_DoesNotThrow()
    {
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = call => call.Sql.StartsWith("INSERT") ? throw new DatabaseException("unique key violation") : 0,
            OnQuery = _ => ["Notification.Schema.v1"] // the other runner's insert is now visible
        };
        var repository = CreateRepository(executor);
        var migration = new AppliedMigration { Id = "H1", MigrationId = "Notification.Schema.v1", Version = 1, AppliedOnUtc = DateTimeOffset.UtcNow };

        await repository.RecordAppliedAsync(migration); // must not throw
    }

    [Fact]
    public async Task RecordAppliedAsync_InsertFailsAndIdStillMissing_Rethrows()
    {
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = call => call.Sql.StartsWith("INSERT") ? throw new DatabaseException("real infrastructure failure") : 0,
            OnQuery = _ => []
        };
        var repository = CreateRepository(executor);
        var migration = new AppliedMigration { Id = "H1", MigrationId = "Notification.Schema.v1", Version = 1, AppliedOnUtc = DateTimeOffset.UtcNow };

        var ex = await Assert.ThrowsAsync<DatabaseException>(() => repository.RecordAppliedAsync(migration));
        Assert.Equal("real infrastructure failure", ex.Message);
    }
}
