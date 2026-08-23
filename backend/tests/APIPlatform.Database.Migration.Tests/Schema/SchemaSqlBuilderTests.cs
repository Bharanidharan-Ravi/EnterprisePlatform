using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Sql;
using APIPlatform.Database.Migration.Sql.Dialects;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Schema;

/// <summary>
/// Asserts the generated DDL text directly — no live database — mirroring
/// MigrationHistorySqlBuilderTests.
/// </summary>
public class SchemaSqlBuilderTests
{
    private static ResolvedTable Resolve(TableDefinition definition, IMigrationSqlDialect dialect)
    {
        Assert.True(TableDefinitionResolver.TryResolve(definition, dialect, out var resolved, out var error), error);
        return resolved;
    }

    [Fact]
    public void CreateTable_SqlServer_UsesBracketQuotingAndPrimaryKeyConstraint()
    {
        var dialect = new SqlServerMigrationDialect();
        var table = Resolve(new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Code", MaxLength = 32, Nullable = false }],
            IncludeAudit = false,
            IncludeAdditionalData = false
        }, dialect);

        var sql = SchemaSqlBuilder.CreateTable(dialect, table);

        Assert.StartsWith("CREATE TABLE [Thing] (", sql);
        Assert.Contains("[Id] NVARCHAR(36) NOT NULL", sql);
        Assert.Contains("[Code] NVARCHAR(32) NOT NULL", sql);
        Assert.Contains("CONSTRAINT [PK_Thing] PRIMARY KEY ([Id])", sql);
    }

    /// <summary>Same request, different provider: HANA needs COLUMN TABLE, double quotes, and
    /// NCLOB rather than NVARCHAR(MAX).</summary>
    [Fact]
    public void CreateTable_Hana_UsesColumnTableAndHanaTypes()
    {
        var dialect = new HanaMigrationDialect();
        var table = Resolve(new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Payload", Type = "json" }],
            IncludeAudit = false
        }, dialect);

        var sql = SchemaSqlBuilder.CreateTable(dialect, table);

        Assert.StartsWith("CREATE COLUMN TABLE \"Thing\" (", sql);
        Assert.Contains("\"Payload\" NCLOB NULL", sql);
        Assert.DoesNotContain("NVARCHAR(MAX)", sql);
    }

    [Fact]
    public void CreateTable_MapsEveryLogicalTypeToAProviderType()
    {
        var dialect = new SqlServerMigrationDialect();
        var table = Resolve(new TableDefinition
        {
            Table = "Thing",
            IncludeAudit = false,
            IncludeAdditionalData = false,
            Fields =
            [
                new FieldDefinition { Name = "S", Type = "string", MaxLength = 10 },
                new FieldDefinition { Name = "T", Type = "text" },
                new FieldDefinition { Name = "I", Type = "int" },
                new FieldDefinition { Name = "L", Type = "long" },
                new FieldDefinition { Name = "B", Type = "bool" },
                new FieldDefinition { Name = "D", Type = "datetime" },
                new FieldDefinition { Name = "M", Type = "decimal" },
                new FieldDefinition { Name = "G", Type = "guid" }
            ]
        }, dialect);

        var sql = SchemaSqlBuilder.CreateTable(dialect, table);

        Assert.Contains("[S] NVARCHAR(10)", sql);
        Assert.Contains("[T] NVARCHAR(MAX)", sql);
        Assert.Contains("[I] INT", sql);
        Assert.Contains("[L] BIGINT", sql);
        Assert.Contains("[B] BIT", sql);
        Assert.Contains("[D] DATETIME2(3)", sql);
        Assert.Contains("[M] DECIMAL(18, 4)", sql);
        Assert.Contains("[G] NVARCHAR(36)", sql);
    }

    [Fact]
    public void CreateIndexes_EmitsUniqueAndPlainIndexes_ButNothingForOrdinaryColumns()
    {
        var dialect = new SqlServerMigrationDialect();
        var table = Resolve(new TableDefinition
        {
            Table = "Thing",
            IncludeAudit = false,
            IncludeAdditionalData = false,
            Fields =
            [
                new FieldDefinition { Name = "Code", Unique = true },
                new FieldDefinition { Name = "Category", Indexed = true },
                new FieldDefinition { Name = "Notes" }
            ]
        }, dialect);

        var indexes = SchemaSqlBuilder.CreateIndexes(dialect, table.TableName, table.Columns).ToList();

        Assert.Equal(2, indexes.Count);
        Assert.Contains(indexes, i => i == "CREATE UNIQUE INDEX [UQ_Thing_Code] ON [Thing] ([Code])");
        Assert.Contains(indexes, i => i == "CREATE INDEX [IX_Thing_Category] ON [Thing] ([Category])");
        Assert.DoesNotContain(indexes, i => i.Contains("Notes"));
    }

    [Fact]
    public void AddColumns_SqlServer_UsesBareAdd()
    {
        var dialect = new SqlServerMigrationDialect();
        var columns = new[] { new ResolvedColumn("Email", "NVARCHAR(256)", true, false, false, false) };

        Assert.Equal("ALTER TABLE [Logins] ADD [Email] NVARCHAR(256) NULL",
            SchemaSqlBuilder.AddColumns(dialect, "Logins", columns));
    }

    /// <summary>HANA requires the parenthesized ADD form; SQL Server rejects it.</summary>
    [Fact]
    public void AddColumns_Hana_UsesParenthesizedAdd()
    {
        var dialect = new HanaMigrationDialect();
        var columns = new[] { new ResolvedColumn("Email", "NVARCHAR(256)", true, false, false, false) };

        Assert.Equal("ALTER TABLE \"Logins\" ADD (\"Email\" NVARCHAR(256) NULL)",
            SchemaSqlBuilder.AddColumns(dialect, "Logins", columns));
    }

    [Fact]
    public void AddColumns_PutsEveryColumnInOneStatement()
    {
        var dialect = new SqlServerMigrationDialect();
        var columns = new[]
        {
            new ResolvedColumn("Email", "NVARCHAR(256)", true, false, false, false),
            new ResolvedColumn("Phone", "NVARCHAR(32)", true, false, false, false)
        };

        var sql = SchemaSqlBuilder.AddColumns(dialect, "Logins", columns);

        Assert.Equal("ALTER TABLE [Logins] ADD [Email] NVARCHAR(256) NULL, [Phone] NVARCHAR(32) NULL", sql);
    }

    [Fact]
    public void DropTable_QuotesTheIdentifier()
    {
        Assert.Equal("DROP TABLE [Logins]", SchemaSqlBuilder.DropTable(new SqlServerMigrationDialect(), "Logins"));
    }

    /// <summary>
    /// Unlike the migration engine's own history table, this table name comes from a request body,
    /// so it must be bound as a parameter rather than inlined into the text.
    /// </summary>
    [Fact]
    public void CatalogQueries_ParameterizeTheTableName_AndWorkOnBothProviders()
    {
        Assert.Contains("@TableName", SchemaSqlBuilder.TableExists());
        Assert.Contains("@TableName", SchemaSqlBuilder.SelectColumnNames());

        Assert.Contains("INFORMATION_SCHEMA.TABLES", SchemaSqlBuilder.TableExists());
        Assert.Contains("INFORMATION_SCHEMA.COLUMNS", SchemaSqlBuilder.SelectColumnNames());

        Assert.DoesNotContain("sys.tables", SchemaSqlBuilder.TableExists(), StringComparison.OrdinalIgnoreCase);
    }
}
