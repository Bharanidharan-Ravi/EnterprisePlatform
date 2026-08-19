using APIPlatform.Database.Migration.Sql;
using APIPlatform.Database.Migration.Sql.Dialects;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Sql;

/// <summary>
/// Asserts the exact SQL text MigrationHistorySqlBuilder produces, for both dialects, without
/// any database — same "pure text generation" split NotificationSqlBuilderTests uses.
/// </summary>
public class MigrationHistorySqlBuilderTests
{
    [Fact]
    public void CreateHistoryTable_SqlServer_UsesTableKeywordAndBracketQuoting()
    {
        var sql = MigrationHistorySqlBuilder.CreateHistoryTable(new SqlServerMigrationDialect());

        Assert.Contains("CREATE TABLE [MigrationHistory]", sql);
        Assert.Contains("DATETIME2(3)", sql);
        Assert.Contains("[Version] INT NOT NULL", sql);
        Assert.Contains("CONSTRAINT [PK_MigrationHistory] PRIMARY KEY ([Id])", sql);
        Assert.Contains("CONSTRAINT [UQ_MigrationHistory_MigrationId] UNIQUE ([MigrationId])", sql);
        Assert.DoesNotContain("IDENTITY", sql);
        Assert.DoesNotContain("NEWID", sql);
        Assert.DoesNotContain("GETDATE", sql);
    }

    [Fact]
    public void CreateHistoryTable_Hana_UsesColumnTableKeywordAndDoubleQuoteQuoting()
    {
        var sql = MigrationHistorySqlBuilder.CreateHistoryTable(new HanaMigrationDialect());

        Assert.Contains("CREATE COLUMN TABLE \"MigrationHistory\"", sql);
        Assert.Contains("TIMESTAMP", sql);
        Assert.Contains("\"Version\" INTEGER NOT NULL", sql);
        Assert.Contains("CONSTRAINT \"PK_MigrationHistory\" PRIMARY KEY (\"Id\")", sql);
        Assert.Contains("CONSTRAINT \"UQ_MigrationHistory_MigrationId\" UNIQUE (\"MigrationId\")", sql);
    }

    [Fact]
    public void TableExists_QueriesInformationSchema_PortableAcrossBothEngines()
    {
        var sql = MigrationHistorySqlBuilder.TableExists();

        Assert.Contains("INFORMATION_SCHEMA.TABLES", sql);
        Assert.Contains("MigrationHistory", sql);
        // Not SQL-Server-only (sys.tables) or HANA-only (SYS.TABLES) — one query works everywhere.
        Assert.DoesNotContain("sys.tables", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectAppliedMigrationIds_SelectsOnlyMigrationIdColumn()
    {
        var sql = MigrationHistorySqlBuilder.SelectAppliedMigrationIds(new SqlServerMigrationDialect());

        Assert.Equal("SELECT [MigrationId] FROM [MigrationHistory]", sql);
    }

    [Fact]
    public void InsertAppliedMigration_ProducesAllFiveColumns()
    {
        var sql = MigrationHistorySqlBuilder.InsertAppliedMigration(new SqlServerMigrationDialect());

        Assert.Contains("INSERT INTO [MigrationHistory]", sql);
        Assert.Contains("@Id", sql);
        Assert.Contains("@MigrationId", sql);
        Assert.Contains("@Version", sql);
        Assert.Contains("@Description", sql);
        Assert.Contains("@AppliedOnUtc", sql);
    }
}
