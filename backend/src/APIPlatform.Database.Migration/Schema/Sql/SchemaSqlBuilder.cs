using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Schema.Sql;

/// <summary>
/// Pure SQL-text generation for the runtime schema engine — no IDatabaseExecutor or Dapper
/// dependency, exactly like <see cref="Migration.Sql.MigrationHistorySqlBuilder"/>, so every
/// statement this produces can be asserted in a unit test without a live database.
///
/// <para>Every identifier reaching these methods has already passed
/// <see cref="SchemaIdentifier"/> and been mapped through <see cref="ColumnTypeMapper"/> in
/// <see cref="TableDefinitionResolver"/>; quoting here is the second layer, not the first.</para>
/// </summary>
internal static class SchemaSqlBuilder
{
    public static string CreateTable(IMigrationSqlDialect dialect, ResolvedTable table)
    {
        var definitions = table.Columns.Select(c => ColumnDefinition(dialect, c)).ToList();

        var key = table.Columns.FirstOrDefault(c => c.PrimaryKey);
        if (key is not null)
        {
            definitions.Add(
                $"CONSTRAINT {dialect.QuoteIdentifier($"PK_{table.TableName}")} " +
                $"PRIMARY KEY ({dialect.QuoteIdentifier(key.Name)})");
        }

        return $"CREATE {dialect.CreateTableKeyword} {dialect.QuoteIdentifier(table.TableName)} " +
               $"({string.Join(", ", definitions)})";
    }

    /// <summary>
    /// One <c>ALTER TABLE … ADD</c> carrying every new column. Issued as a single statement rather
    /// than one per column so that on SQL Server, where DDL is transactional, the additions land
    /// together — and so HANA, which auto-commits each statement, has one commit point instead of
    /// several partial ones.
    /// </summary>
    public static string AddColumns(IMigrationSqlDialect dialect, string tableName, IReadOnlyList<ResolvedColumn> columns)
    {
        var definitions = string.Join(", ", columns.Select(c => ColumnDefinition(dialect, c)));
        return $"ALTER TABLE {dialect.QuoteIdentifier(tableName)} {dialect.AddColumnClause(definitions)}";
    }

    public static string DropTable(IMigrationSqlDialect dialect, string tableName) =>
        $"DROP TABLE {dialect.QuoteIdentifier(tableName)}";

    public static IEnumerable<string> CreateIndexes(IMigrationSqlDialect dialect, string tableName, IEnumerable<ResolvedColumn> columns)
    {
        foreach (var column in columns)
        {
            // Unique already builds an index, so Indexed adds nothing on top of it.
            if (!column.Unique && !column.Indexed) continue;

            var unique = column.Unique ? "UNIQUE " : string.Empty;
            var prefix = column.Unique ? "UQ" : "IX";
            yield return
                $"CREATE {unique}INDEX {dialect.QuoteIdentifier($"{prefix}_{tableName}_{column.Name}")} " +
                $"ON {dialect.QuoteIdentifier(tableName)} ({dialect.QuoteIdentifier(column.Name)})";
        }
    }

    /// <summary>
    /// Existence check via INFORMATION_SCHEMA.TABLES, which both SQL Server and SAP HANA support
    /// (unlike sys.tables/SYS.TABLES) — no dialect branch needed. Unlike
    /// <see cref="Migration.Sql.MigrationHistorySqlBuilder.TableExists"/>, whose table name is a
    /// compile-time constant, the name here comes from a request body, so it is bound as the
    /// parameter <c>@TableName</c> rather than inlined.
    /// </summary>
    public static string TableExists() =>
        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";

    /// <summary>Existing column names for a table, used to work out which requested columns are
    /// actually missing before an ALTER is built.</summary>
    public static string SelectColumnNames() =>
        "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";

    private static string ColumnDefinition(IMigrationSqlDialect dialect, ResolvedColumn column) =>
        $"{dialect.QuoteIdentifier(column.Name)} {column.SqlType} {(column.Nullable ? "NULL" : "NOT NULL")}";
}
