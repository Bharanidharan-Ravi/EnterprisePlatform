using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Sql;

/// <summary>
/// Pure SQL-text generation for the migration engine's own bookkeeping table
/// (<c>MigrationHistory</c>). Kept free of any IDatabaseExecutor/Dapper dependency, exactly like
/// APIPlatform.Notification's NotificationSqlBuilder, so the generated SQL can be asserted
/// directly in unit tests without a live database.
/// </summary>
internal static class MigrationHistorySqlBuilder
{
    public const string TableName = "MigrationHistory";

    private static readonly string[] Columns = ["Id", "MigrationId", "Version", "Description", "AppliedOnUtc"];

    /// <summary>
    /// Full CREATE TABLE for MigrationHistory. No IDENTITY, no NEWID(), no GETDATE()/DEFAULT time
    /// expression, no MERGE — Id and AppliedOnUtc are always supplied by the caller
    /// (MigrationRunner, API-generated), matching every other platform table.
    /// </summary>
    public static string CreateHistoryTable(IMigrationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(TableName);
        var id = dialect.QuoteIdentifier("Id");
        var migrationId = dialect.QuoteIdentifier("MigrationId");
        var version = dialect.QuoteIdentifier("Version");
        var description = dialect.QuoteIdentifier("Description");
        var appliedOnUtc = dialect.QuoteIdentifier("AppliedOnUtc");
        var pk = dialect.QuoteIdentifier($"PK_{TableName}");
        var unique = dialect.QuoteIdentifier($"UQ_{TableName}_MigrationId");

        return
            $"CREATE {dialect.CreateTableKeyword} {table} (" +
            $"{id} NVARCHAR(36) NOT NULL, " +
            $"{migrationId} NVARCHAR(200) NOT NULL, " +
            $"{version} {dialect.IntegerType} NOT NULL, " +
            $"{description} NVARCHAR(400) NULL, " +
            $"{appliedOnUtc} {dialect.TimestampType} NOT NULL, " +
            $"CONSTRAINT {pk} PRIMARY KEY ({id}), " +
            $"CONSTRAINT {unique} UNIQUE ({migrationId}))";
    }

    /// <summary>
    /// Existence check via INFORMATION_SCHEMA.TABLES — supported by both SQL Server and SAP HANA
    /// (unlike sys.tables/SYS.TABLES, which are engine-specific), so no dialect branch is needed
    /// here. The table name is this package's own compile-time constant, never caller input, so
    /// it's safe to inline rather than parameterize.
    /// </summary>
    public static string TableExists() =>
        $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TableName}'";

    public static string SelectAppliedMigrationIds(IMigrationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(TableName);
        var migrationId = dialect.QuoteIdentifier("MigrationId");
        return $"SELECT {migrationId} FROM {table}";
    }

    public static string InsertAppliedMigration(IMigrationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(TableName);
        var columns = string.Join(", ", Columns.Select(dialect.QuoteIdentifier));
        var parameters = string.Join(", ", Columns.Select(c => "@" + c));
        return $"INSERT INTO {table} ({columns}) VALUES ({parameters})";
    }
}
