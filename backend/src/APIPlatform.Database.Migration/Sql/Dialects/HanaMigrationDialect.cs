namespace APIPlatform.Database.Migration.Sql.Dialects;

/// <summary>SAP HANA dialect. HANA DDL statements auto-commit and are not part of any
/// surrounding transaction — a failed HANA migration can leave earlier statements in this same
/// migration already committed. Keep HANA migrations additive and idempotent-by-history-tracking
/// (never re-run once the MigrationHistory row exists) rather than relying on rollback.</summary>
internal sealed class HanaMigrationDialect : IMigrationSqlDialect
{
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public bool SupportsTransactionalDdl => false;

    public string CreateTableKeyword => "COLUMN TABLE";

    public string TimestampType => "TIMESTAMP";

    public string IntegerType => "INTEGER";
}
