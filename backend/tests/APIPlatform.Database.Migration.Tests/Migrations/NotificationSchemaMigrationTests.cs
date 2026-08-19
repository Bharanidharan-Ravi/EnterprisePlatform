using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Migrations.Notification;
using APIPlatform.Database.Migration.Tests.Fakes;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Migrations;

/// <summary>
/// Asserts the generated DDL text contains every table/column/index name
/// APIPlatform.Notification's NotificationSqlBuilder and repository actually rely on, for both
/// providers — this is Phase 9's "Notification schema compatibility with the actual repository
/// queries" check, done at the generated-SQL level since no live database is available here.
/// </summary>
public class NotificationSchemaMigrationTests
{
    // Columns NotificationSqlBuilder.InsertNotification/RecipientMatch/EntityHistory select/bind.
    private static readonly string[] NotificationColumns =
        ["Id", "Application", "EntityType", "EntityId", "EventType", "Title", "Message", "Data", "CreatedBy", "CreatedOnUtc"];

    // Columns NotificationSqlBuilder.InsertTarget/RecipientMatch bind/filter on.
    private static readonly string[] TargetColumns = ["Id", "NotificationId", "TargetKind", "TargetValue", "IsExclusion"];

    // Columns NotificationSqlBuilder.GetUserState/UpdateLastReadOn/UpdateLastSyncedOn select/bind.
    private static readonly string[] UserStateColumns = ["UserId", "Application", "LastReadOnUtc", "LastSyncedOnUtc", "UpdatedOnUtc"];

    [Fact]
    public void SqlServerStatements_CreateAllThreeTables()
    {
        var sql = string.Join(" ", NotificationSchemaSql.SqlServerStatements);

        Assert.Contains("CREATE TABLE [Notifications]", sql);
        Assert.Contains("CREATE TABLE [NotificationTargets]", sql);
        Assert.Contains("CREATE TABLE [NotificationUserStates]", sql);
        Assert.All(NotificationColumns, c => Assert.Contains($"[{c}]", sql));
        Assert.All(TargetColumns, c => Assert.Contains($"[{c}]", sql));
        Assert.All(UserStateColumns, c => Assert.Contains($"[{c}]", sql));
    }

    [Fact]
    public void SqlServerStatements_CreateEveryIndexTheRepositoryReliesOn()
    {
        var sql = string.Join(" ", NotificationSchemaSql.SqlServerStatements);

        // Anchors NotificationSqlBuilder.RecipientMatch's Application(+Since) filter.
        Assert.Contains("[IX_Notifications_Application_CreatedOnUtc]", sql);
        // Anchors NotificationSqlBuilder.EntityHistory's Application/EntityType/EntityId filter.
        Assert.Contains("[IX_Notifications_Entity]", sql);
        // Anchors NotificationSqlBuilder.RecipientMatch's EXISTS/NOT EXISTS seeks.
        Assert.Contains("[IX_NotificationTargets_Notification]", sql);
    }

    [Fact]
    public void SqlServerStatements_NoIdentityNewIdGetDateOrMerge()
    {
        var sql = string.Join(" ", NotificationSchemaSql.SqlServerStatements).ToUpperInvariant();

        Assert.DoesNotContain("IDENTITY", sql);
        Assert.DoesNotContain("NEWID(", sql);
        Assert.DoesNotContain("GETDATE(", sql);
        Assert.DoesNotContain("MERGE ", sql);
    }

    [Fact]
    public void HanaStatements_CreateAllThreeTables_WithColumnTableAndDoubleQuoteIdentifiers()
    {
        var sql = string.Join(" ", NotificationSchemaSql.HanaStatements);

        Assert.Contains("CREATE COLUMN TABLE \"Notifications\"", sql);
        Assert.Contains("CREATE COLUMN TABLE \"NotificationTargets\"", sql);
        Assert.Contains("CREATE COLUMN TABLE \"NotificationUserStates\"", sql);
        Assert.All(NotificationColumns, c => Assert.Contains($"\"{c}\"", sql));
        Assert.All(TargetColumns, c => Assert.Contains($"\"{c}\"", sql));
        Assert.All(UserStateColumns, c => Assert.Contains($"\"{c}\"", sql));
    }

    [Fact]
    public void HanaStatements_UsesHanaTypesNotSqlServerTypes()
    {
        var sql = string.Join(" ", NotificationSchemaSql.HanaStatements);

        Assert.Contains("NCLOB", sql);       // NVARCHAR(MAX) -> NCLOB
        Assert.Contains("TIMESTAMP", sql);   // DATETIME2(3) -> TIMESTAMP
        Assert.Contains("BOOLEAN", sql);     // BIT -> BOOLEAN
        Assert.DoesNotContain("NVARCHAR(MAX)", sql);
        Assert.DoesNotContain("DATETIME2", sql);
    }

    [Fact]
    public async Task SqlServerMigration_ApplyAsync_ExecutesEveryStatement_InOrder_UsingTheGivenTransaction()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 0 };
        var transaction = new FakeDatabaseTransaction();
        var migration = new NotificationSqlServerMigration();

        await migration.ApplyAsync(executor, transaction);

        Assert.Equal(NotificationSchemaSql.SqlServerStatements.Length, executor.ExecuteCalls.Count);
        Assert.All(executor.ExecuteCalls, c => Assert.Same(transaction, c.Transaction));
        Assert.Equal(DatabaseProvider.SqlServer, migration.SupportedProvider);
    }

    [Fact]
    public async Task HanaMigration_ApplyAsync_ExecutesEveryStatement_WithNoTransaction()
    {
        var executor = new FakeDatabaseExecutor { OnExecute = _ => 0 };
        var migration = new NotificationHanaMigration();

        await migration.ApplyAsync(executor, transaction: null);

        Assert.Equal(NotificationSchemaSql.HanaStatements.Length, executor.ExecuteCalls.Count);
        Assert.All(executor.ExecuteCalls, c => Assert.Null(c.Transaction));
        Assert.Equal(DatabaseProvider.Hana, migration.SupportedProvider);
    }

    [Fact]
    public void SqlServerAndHanaMigrations_ShareTheSameMigrationIdAndVersion()
    {
        // One logical migration, tracked as a single MigrationHistory row regardless of which
        // provider variant actually applied it.
        var sqlServer = new NotificationSqlServerMigration();
        var hana = new NotificationHanaMigration();

        Assert.Equal(sqlServer.MigrationId, hana.MigrationId);
        Assert.Equal(sqlServer.Version, hana.Version);
    }
}
