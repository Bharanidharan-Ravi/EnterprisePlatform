namespace APIPlatform.Database.Migration.Sql.Dialects;

/// <summary>Provider-specific SQL text concerns for the migration engine itself — identifier
/// quoting for the MigrationHistory table, and whether DDL participates in transactions on this
/// engine. Mirrors the same "small dialect abstraction at the call site" pattern
/// APIPlatform.Notification and APIPlatform.CrudEngine already use.</summary>
public interface IMigrationSqlDialect
{
    string QuoteIdentifier(string identifier);

    /// <summary>
    /// True when <c>CREATE TABLE</c>/<c>CREATE INDEX</c> can be rolled back as part of an
    /// ordinary transaction on this engine (SQL Server). False when DDL auto-commits regardless
    /// of any surrounding transaction (SAP HANA) — <see cref="Services.MigrationRunner"/> uses
    /// this to decide whether wrapping a migration's DDL in a transaction provides real rollback
    /// safety, or would just be misleading.
    /// </summary>
    bool SupportsTransactionalDdl { get; }

    /// <summary>"TABLE" (SQL Server) or "COLUMN TABLE" (SAP HANA) — the keyword that follows
    /// CREATE when defining a new table.</summary>
    string CreateTableKeyword { get; }

    /// <summary>Column type for an API-generated UTC instant: "DATETIME2(3)" or "TIMESTAMP".</summary>
    string TimestampType { get; }

    /// <summary>Column type for a plain integer: "INT" or "INTEGER".</summary>
    string IntegerType { get; }
}
