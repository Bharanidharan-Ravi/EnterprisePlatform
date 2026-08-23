using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Services;
using APIPlatform.Database.Migration.Sql.Dialects;
using APIPlatform.Database.Migration.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Schema;

/// <summary>
/// Covers what the service decides before and after it builds SQL — whether an operation applies
/// at all, and which columns an update actually touches. The DDL text itself is asserted in
/// <see cref="SchemaSqlBuilderTests"/>.
/// </summary>
public class SchemaMigrationServiceTests
{
    private static (SchemaMigrationService Service, FakeDatabaseExecutor Executor) Build(
        bool tableExists = false,
        IEnumerable<string>? existingColumns = null,
        DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        var executor = new FakeDatabaseExecutor
        {
            OnExecuteScalar = _ => tableExists ? 1 : 0,
            OnQuery = _ => existingColumns ?? []
        };

        var resolver = new MigrationSqlDialectResolver(Options.Create(new DatabaseOptions
        {
            ConnectionString = "unused",
            Provider = provider
        }));

        return (new SchemaMigrationService(executor, resolver), executor);
    }

    /// <summary>DDL statements only — the existence probe goes through ExecuteScalar, which the
    /// fake also records in ExecuteCalls.</summary>
    private static List<string> DdlStatements(FakeDatabaseExecutor executor) =>
        executor.ExecuteCalls
            .Select(c => c.Sql)
            .Where(sql => !sql.Contains("INFORMATION_SCHEMA"))
            .ToList();

    [Fact]
    public async Task CreateTable_FromTemplate_CreatesTableAndItsIndexes()
    {
        var (service, executor) = Build(tableExists: false);

        var result = await service.CreateTableAsync(new TableDefinition { Template = "login" });

        Assert.Equal(SchemaOperationStatus.Success, result.Status);
        Assert.Equal("Logins", result.Table);
        Assert.Contains("Username", result.Columns);
        Assert.Contains("CreatedOnUtc", result.Columns);

        var ddl = DdlStatements(executor);
        Assert.Contains(ddl, s => s.StartsWith("CREATE TABLE [Logins]"));
        Assert.Contains(ddl, s => s.Contains("UQ_Logins_Username"));
    }

    [Fact]
    public async Task CreateTable_WhenTableAlreadyExists_IsAConflictAndRunsNoDdl()
    {
        var (service, executor) = Build(tableExists: true);

        var result = await service.CreateTableAsync(new TableDefinition { Template = "login" });

        Assert.Equal(SchemaOperationStatus.Conflict, result.Status);
        Assert.Empty(DdlStatements(executor));
    }

    [Fact]
    public async Task CreateTable_WithInvalidRequest_RunsNothing_AndNeverProbesTheDatabase()
    {
        var (service, executor) = Build();

        var result = await service.CreateTableAsync(new TableDefinition { Table = "Users; DROP TABLE Logins--" });

        Assert.Equal(SchemaOperationStatus.Invalid, result.Status);
        Assert.Empty(executor.ExecuteCalls);
        Assert.Empty(executor.QueryCalls);
    }

    [Fact]
    public async Task CreateTable_OnSqlServer_WrapsDdlInATransaction()
    {
        var (service, executor) = Build(provider: DatabaseProvider.SqlServer);

        await service.CreateTableAsync(new TableDefinition { Template = "login" });

        Assert.Equal(1, executor.BeginTransactionCallCount);
    }

    /// <summary>HANA auto-commits DDL, so opening a transaction would imply a rollback guarantee
    /// the engine cannot provide — same reasoning as MigrationRunner.</summary>
    [Fact]
    public async Task CreateTable_OnHana_RunsWithoutATransaction()
    {
        var (service, executor) = Build(provider: DatabaseProvider.Hana);

        await service.CreateTableAsync(new TableDefinition { Template = "login" });

        Assert.Equal(0, executor.BeginTransactionCallCount);
    }

    [Fact]
    public async Task UpdateTable_AddsOnlyTheColumnsThatAreMissing()
    {
        var (service, executor) = Build(
            tableExists: true,
            existingColumns: ["Id", "Username", "CreatedOnUtc"]);

        var result = await service.UpdateTableAsync(new TableDefinition
        {
            Table = "Employees",
            Fields =
            [
                new FieldDefinition { Name = "Username" },      // already there
                new FieldDefinition { Name = "EmployeeCode" }   // new
            ],
            IncludeAudit = false,
            IncludeAdditionalData = false
        });

        Assert.Equal(SchemaOperationStatus.Success, result.Status);
        Assert.Equal(["EmployeeCode"], result.Columns);

        var alter = Assert.Single(DdlStatements(executor));
        Assert.Contains("EmployeeCode", alter);
        Assert.DoesNotContain("[Username]", alter);
    }

    /// <summary>A NOT NULL column cannot be added to a table that may already have rows, so the
    /// request's nullable:false is overridden rather than emitted as DDL that fails.</summary>
    [Fact]
    public async Task UpdateTable_AddsNewColumnsAsNullable_EvenWhenRequestedNotNull()
    {
        var (service, executor) = Build(tableExists: true, existingColumns: ["Id"]);

        await service.UpdateTableAsync(new TableDefinition
        {
            Table = "Employees",
            Fields = [new FieldDefinition { Name = "EmployeeCode", Nullable = false }],
            IncludeAudit = false,
            IncludeAdditionalData = false
        });

        Assert.Contains("[EmployeeCode] NVARCHAR(200) NULL", Assert.Single(DdlStatements(executor)));
    }

    [Fact]
    public async Task UpdateTable_WhenNothingIsMissing_ReportsNoChange_AndRunsNoDdl()
    {
        var (service, executor) = Build(
            tableExists: true,
            existingColumns: ["Id", "Code", "AdditionalData", "CreatedBy", "CreatedOnUtc", "LastModifiedBy", "LastModifiedOnUtc"]);

        var result = await service.UpdateTableAsync(new TableDefinition
        {
            Table = "Things",
            Fields = [new FieldDefinition { Name = "Code" }]
        });

        Assert.Equal(SchemaOperationStatus.NoChange, result.Status);
        Assert.Empty(DdlStatements(executor));
    }

    [Fact]
    public async Task UpdateTable_OnAMissingTable_IsAConflict()
    {
        var (service, executor) = Build(tableExists: false);

        var result = await service.UpdateTableAsync(new TableDefinition
        {
            Table = "Logins",
            Fields = [new FieldDefinition { Name = "EmployeeCode" }]
        });

        Assert.Equal(SchemaOperationStatus.Conflict, result.Status);
        Assert.Empty(DdlStatements(executor));
    }

    [Fact]
    public async Task DeleteTable_DropsTheTable()
    {
        var (service, executor) = Build(tableExists: true);

        var result = await service.DeleteTableAsync("Logins");

        Assert.Equal(SchemaOperationStatus.Success, result.Status);
        Assert.Equal("DROP TABLE [Logins]", Assert.Single(DdlStatements(executor)));
    }

    [Fact]
    public async Task DeleteTable_OnAMissingTable_IsAConflictAndDropsNothing()
    {
        var (service, executor) = Build(tableExists: false);

        var result = await service.DeleteTableAsync("Logins");

        Assert.Equal(SchemaOperationStatus.Conflict, result.Status);
        Assert.Empty(DdlStatements(executor));
    }

    [Fact]
    public async Task DeleteTable_WithAnInvalidName_DropsNothing()
    {
        var (service, executor) = Build(tableExists: true);

        var result = await service.DeleteTableAsync("Logins]; DROP TABLE Users--");

        Assert.Equal(SchemaOperationStatus.Invalid, result.Status);
        Assert.Empty(executor.ExecuteCalls);
    }
}
