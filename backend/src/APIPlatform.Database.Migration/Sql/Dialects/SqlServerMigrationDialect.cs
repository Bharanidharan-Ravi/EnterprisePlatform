namespace APIPlatform.Database.Migration.Sql.Dialects;

/// <summary>T-SQL dialect. SQL Server supports transactional DDL — CREATE TABLE/CREATE INDEX
/// roll back cleanly with the rest of the transaction if a later statement fails.</summary>
internal sealed class SqlServerMigrationDialect : IMigrationSqlDialect
{
    public string QuoteIdentifier(string identifier) => $"[{identifier}]";

    public bool SupportsTransactionalDdl => true;

    public string CreateTableKeyword => "TABLE";

    public string TimestampType => "DATETIME2(3)";

    public string IntegerType => "INT";
}
