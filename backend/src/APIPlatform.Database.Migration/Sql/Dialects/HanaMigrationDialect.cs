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

    public string BigIntType => "BIGINT";

    public string BooleanType => "BOOLEAN";

    public string DecimalType => "DECIMAL(18, 4)";

    public string UnboundedTextType => "NCLOB";

    public string StringType(int maxLength) => $"NVARCHAR({maxLength})";

    /// <summary>HANA requires the parenthesized form — <c>ALTER TABLE t ADD (col TYPE)</c> —
    /// unlike SQL Server's bare <c>ADD col TYPE</c>.</summary>
    public string AddColumnClause(string columnDefinitions) => $"ADD ({columnDefinitions})";
}
