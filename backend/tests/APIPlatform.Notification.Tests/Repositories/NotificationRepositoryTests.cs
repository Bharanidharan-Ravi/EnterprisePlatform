using APIPlatform.Data.Exceptions;
using APIPlatform.Notification.Models;
using APIPlatform.Notification.Repositories;
using APIPlatform.Notification.Sql.Dialects;
using APIPlatform.Notification.Tests.Fakes;
using Xunit;

namespace APIPlatform.Notification.Tests.Repositories;

/// <summary>
/// Exercises NotificationRepository's control flow (transaction usage, parameter binding, the
/// update-first/insert-fallback upsert race handling) against FakeDatabaseExecutor — whitebox,
/// relying on InternalsVisibleTo, since the SQL generation and row-mapping types are intentionally
/// not part of the module's public surface. No live SQL Server/HANA instance is needed, matching
/// how APIPlatform.Database.Tests itself stays runnable without one.
/// </summary>
public class NotificationRepositoryTests
{
    private sealed class StaticDialectResolver(INotificationSqlDialect dialect) : INotificationSqlDialectResolver
    {
        public INotificationSqlDialect Resolve() => dialect;
    }

    private static NotificationRepository CreateRepository(FakeDatabaseExecutor executor) =>
        new(executor, new StaticDialectResolver(new SqlServerNotificationDialect()));

    [Fact]
    public async Task InsertAsync_InsertsNotificationAndEveryTarget_ThenCommits()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 1 };
        FakeDatabaseTransaction? transaction = null;
        executor.OnBeginTransaction = () => transaction = new FakeDatabaseTransaction();
        var repository = CreateRepository(executor);

        var notification = new NotificationRecord
        {
            Id = "N1", Application = "PROJECT", EventType = "PROJECT_CREATED", Title = "Created",
            CreatedOnUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var targets = new[] { NotificationTargetRule.TargetGroup("PROJECT_TEAM"), NotificationTargetRule.ExcludeUser("U007") };

        await repository.InsertAsync(notification, targets);

        Assert.Equal(3, executor.ExecuteCalls.Count); // 1 notification insert + 2 target inserts
        Assert.Contains("INSERT INTO [Notifications]", executor.ExecuteCalls[0].Sql);
        Assert.All(executor.ExecuteCalls.Skip(1), c => Assert.Contains("INSERT INTO [NotificationTargets]", c.Sql));
        Assert.All(executor.ExecuteCalls, c => Assert.Same(transaction, c.Transaction));
        Assert.True(transaction!.Committed);
        Assert.False(transaction.RolledBack);
    }

    [Fact]
    public async Task InsertAsync_WhenATargetInsertFails_TransactionIsNeverCommitted()
    {
        var callCount = 0;
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = _ =>
            {
                callCount++;
                if (callCount == 2) throw new DatabaseException("simulated failure");
                return 1;
            }
        };
        FakeDatabaseTransaction? transaction = null;
        executor.OnBeginTransaction = () => transaction = new FakeDatabaseTransaction();
        var repository = CreateRepository(executor);

        var notification = new NotificationRecord
        {
            Id = "N1", Application = "PROJECT", EventType = "E", Title = "T", CreatedOnUtc = DateTimeOffset.UtcNow
        };
        var targets = new[] { NotificationTargetRule.TargetAll() };

        await Assert.ThrowsAsync<DatabaseException>(() => repository.InsertAsync(notification, targets));

        Assert.False(transaction!.Committed);
    }

    [Fact]
    public async Task ListForRecipientAsync_BindsApplicationUserIdSinceAndGroupCodes()
    {
        var executor = new FakeDatabaseExecutor { OnQuery = _ => Array.Empty<NotificationRow>() };
        var repository = CreateRepository(executor);
        var since = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var recipient = NotificationRecipient.For("U1", ["G1", "G2"]);

        await repository.ListForRecipientAsync("PROJECT", recipient, since, skip: 0, take: 20);

        var call = executor.QueryCalls.Single();
        Assert.Equal("PROJECT", call.Parameters!["Application"]);
        Assert.Equal("U1", call.Parameters["UserId"]);
        Assert.Equal(since.UtcDateTime, call.Parameters["Since"]);
        Assert.Equal("G1", call.Parameters["g0"]);
        Assert.Equal("G2", call.Parameters["g1"]);
    }

    [Fact]
    public async Task ListForRecipientAsync_MapsRowsToUtcDateTimeOffset()
    {
        var row = new NotificationRow
        {
            Id = "N1", Application = "PROJECT", EventType = "E", Title = "T",
            CreatedOnUtc = new DateTime(2026, 6, 1, 8, 30, 0, DateTimeKind.Unspecified)
        };
        var executor = new FakeDatabaseExecutor { OnQuery = _ => [row] };
        var repository = CreateRepository(executor);

        var result = await repository.ListForRecipientAsync("PROJECT", NotificationRecipient.For("U1"), null, 0, 20);

        Assert.Equal(TimeSpan.Zero, result[0].CreatedOnUtc.Offset);
        Assert.Equal(row.CreatedOnUtc, result[0].CreatedOnUtc.DateTime);
    }

    [Fact]
    public async Task CountForRecipientAsync_UsesExecuteScalar()
    {
        var executor = new FakeDatabaseExecutor { OnExecuteScalar = _ => 7 };
        var repository = CreateRepository(executor);

        var count = await repository.CountForRecipientAsync("PROJECT", NotificationRecipient.For("U1"), null);

        Assert.Equal(7, count);
        Assert.StartsWith("SELECT COUNT(*)", executor.ExecuteCalls.Single().Sql);
    }

    [Fact]
    public async Task GetUserStateAsync_NoRow_ReturnsNull()
    {
        var executor = new FakeDatabaseExecutor { OnQuerySingleOrDefault = _ => null };
        var repository = CreateRepository(executor);

        var state = await repository.GetUserStateAsync("PROJECT", "U1");

        Assert.Null(state);
    }

    [Fact]
    public async Task SetLastReadOnAsync_UpdateAffectsRow_DoesNotInsert()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 1 };
        var repository = CreateRepository(executor);

        await repository.SetLastReadOnAsync("PROJECT", "U1", DateTimeOffset.UtcNow);

        Assert.Single(executor.ExecuteCalls);
        Assert.StartsWith("UPDATE", executor.ExecuteCalls[0].Sql);
    }

    [Fact]
    public async Task SetLastReadOnAsync_UpdateAffectsNoRows_FallsBackToInsert()
    {
        var updateCount = 0;
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = call =>
            {
                if (call.Sql.StartsWith("UPDATE")) { updateCount++; return 0; }
                return 1; // INSERT succeeds
            }
        };
        var repository = CreateRepository(executor);

        await repository.SetLastReadOnAsync("PROJECT", "U1", DateTimeOffset.UtcNow);

        Assert.Equal(1, updateCount);
        Assert.Equal(2, executor.ExecuteCalls.Count);
        Assert.StartsWith("INSERT", executor.ExecuteCalls[1].Sql);
    }

    [Fact]
    public async Task SetLastReadOnAsync_InsertRacesAndLoses_RetriesUpdateAndSucceeds()
    {
        var updateCount = 0;
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = call =>
            {
                if (call.Sql.StartsWith("UPDATE"))
                {
                    updateCount++;
                    return updateCount == 1 ? 0 : 1; // first UPDATE misses, retry after the race succeeds
                }
                throw new DatabaseException("unique key violation (someone else inserted first)");
            }
        };
        var repository = CreateRepository(executor);

        await repository.SetLastReadOnAsync("PROJECT", "U1", DateTimeOffset.UtcNow); // must not throw

        Assert.Equal(2, updateCount);
    }

    [Fact]
    public async Task SetLastReadOnAsync_InsertFailsAndRetryUpdateAlsoMisses_RethrowsOriginalException()
    {
        var executor = new FakeDatabaseExecutor
        {
            OnExecute = call =>
            {
                if (call.Sql.StartsWith("UPDATE")) return 0;
                throw new DatabaseException("infrastructure failure");
            }
        };
        var repository = CreateRepository(executor);

        var ex = await Assert.ThrowsAsync<DatabaseException>(() =>
            repository.SetLastReadOnAsync("PROJECT", "U1", DateTimeOffset.UtcNow));
        Assert.Equal("infrastructure failure", ex.Message);
    }

    [Fact]
    public async Task SetLastSyncedOnAsync_DoesNotAffectLastReadOnColumn()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 1 };
        var repository = CreateRepository(executor);

        await repository.SetLastSyncedOnAsync("PROJECT", "U1", DateTimeOffset.UtcNow);

        Assert.Contains("LastSyncedOnUtc", executor.ExecuteCalls[0].Sql);
        Assert.DoesNotContain("LastReadOnUtc", executor.ExecuteCalls[0].Sql);
    }
}
