using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Schema.Abstractions;
using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Sql;
using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Schema.Services;

/// <summary>
/// Default <see cref="ISchemaMigrationService"/>. Every operation follows the same three steps —
/// resolve the request into validated columns, check the live catalog to decide whether the
/// operation is even applicable, then execute the generated DDL — so a template table and a
/// caller-defined table are handled by identical code after
/// <see cref="TableDefinitionResolver"/> has run.
/// </summary>
public sealed class SchemaMigrationService : ISchemaMigrationService
{
    private readonly IDatabaseExecutor _executor;
    private readonly IMigrationSqlDialectResolver _dialectResolver;

    public SchemaMigrationService(IDatabaseExecutor executor, IMigrationSqlDialectResolver dialectResolver)
    {
        _executor = executor;
        _dialectResolver = dialectResolver;
    }

    public async Task<SchemaOperationResult> CreateTableAsync(TableDefinition definition, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();

        if (!TableDefinitionResolver.TryResolve(definition, dialect, out var table, out var error))
            return SchemaOperationResult.Invalid(definition.Table ?? string.Empty, error);

        if (await TableExistsAsync(table.TableName, cancellationToken))
        {
            return SchemaOperationResult.Conflict(table.TableName,
                $"Table '{table.TableName}' already exists. Use the update operation to add new columns to it.");
        }

        var statements = new List<string> { SchemaSqlBuilder.CreateTable(dialect, table) };
        statements.AddRange(SchemaSqlBuilder.CreateIndexes(dialect, table.TableName, table.Columns));

        await ExecuteAsync(dialect, statements, cancellationToken);

        var origin = table.TemplateKey is null
            ? "from the supplied fields"
            : $"from the '{table.TemplateKey}' template";

        return new SchemaOperationResult
        {
            Status = SchemaOperationStatus.Success,
            Table = table.TableName,
            Message = $"Created table '{table.TableName}' {origin} with {table.Columns.Count} columns.",
            Columns = table.Columns.Select(c => c.Name).ToList(),
            ExecutedStatements = statements
        };
    }

    public async Task<SchemaOperationResult> UpdateTableAsync(TableDefinition definition, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();

        if (!TableDefinitionResolver.TryResolve(definition, dialect, out var table, out var error))
            return SchemaOperationResult.Invalid(definition.Table ?? string.Empty, error);

        if (!await TableExistsAsync(table.TableName, cancellationToken))
        {
            return SchemaOperationResult.Conflict(table.TableName,
                $"Table '{table.TableName}' does not exist. Use the create operation first.");
        }

        var existing = await GetColumnNamesAsync(table.TableName, cancellationToken);
        var missing = table.Columns.Where(c => !existing.Contains(c.Name)).ToList();

        if (missing.Count == 0)
        {
            return new SchemaOperationResult
            {
                Status = SchemaOperationStatus.NoChange,
                Table = table.TableName,
                Message = $"Table '{table.TableName}' already has every requested column; nothing to add."
            };
        }

        // A NOT NULL column cannot be added to a table that may already hold rows — there is no
        // value to put in it for those rows. Force every addition nullable rather than emitting DDL
        // that fails at execution time, and say so in the response.
        var additions = missing.Select(c => c with { Nullable = true, PrimaryKey = false }).ToList();

        var statements = new List<string> { SchemaSqlBuilder.AddColumns(dialect, table.TableName, additions) };
        statements.AddRange(SchemaSqlBuilder.CreateIndexes(dialect, table.TableName, additions));

        await ExecuteAsync(dialect, statements, cancellationToken);

        return new SchemaOperationResult
        {
            Status = SchemaOperationStatus.Success,
            Table = table.TableName,
            Message = $"Added {additions.Count} column(s) to '{table.TableName}' as nullable; " +
                      "existing columns were left unchanged.",
            Columns = additions.Select(c => c.Name).ToList(),
            ExecutedStatements = statements
        };
    }

    public async Task<SchemaOperationResult> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        if (!SchemaIdentifier.TryValidate(tableName, "Table", out var error))
            return SchemaOperationResult.Invalid(tableName ?? string.Empty, error);

        var dialect = _dialectResolver.Resolve();

        if (!await TableExistsAsync(tableName, cancellationToken))
            return SchemaOperationResult.Conflict(tableName, $"Table '{tableName}' does not exist.");

        var statements = new List<string> { SchemaSqlBuilder.DropTable(dialect, tableName) };
        await ExecuteAsync(dialect, statements, cancellationToken);

        return new SchemaOperationResult
        {
            Status = SchemaOperationStatus.Success,
            Table = tableName,
            Message = $"Dropped table '{tableName}' and all of its rows.",
            ExecutedStatements = statements
        };
    }

    public async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var count = await _executor.ExecuteScalarAsync<int>(
            SchemaSqlBuilder.TableExists(),
            new Dictionary<string, object?> { ["TableName"] = tableName },
            cancellationToken: cancellationToken);

        return count > 0;
    }

    private async Task<HashSet<string>> GetColumnNamesAsync(string tableName, CancellationToken cancellationToken)
    {
        var names = await _executor.QueryAsync<string>(
            SchemaSqlBuilder.SelectColumnNames(),
            new Dictionary<string, object?> { ["TableName"] = tableName },
            cancellationToken: cancellationToken);

        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Runs the generated DDL, in a transaction where that actually buys something. On SQL Server
    /// a failed index halfway through rolls the CREATE TABLE back with it; on SAP HANA every DDL
    /// statement auto-commits regardless, so opening a transaction there would imply a rollback
    /// guarantee this engine cannot provide — mirrors <see cref="Migration.Services.MigrationRunner"/>.
    /// </summary>
    private async Task ExecuteAsync(IMigrationSqlDialect dialect, IReadOnlyList<string> statements, CancellationToken cancellationToken)
    {
        if (dialect.SupportsTransactionalDdl)
        {
            await using var transaction = await _executor.BeginTransactionAsync(cancellationToken: cancellationToken);
            await ExecuteAllAsync(statements, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await ExecuteAllAsync(statements, transaction: null, cancellationToken);
    }

    private async Task ExecuteAllAsync(IReadOnlyList<string> statements, IDatabaseTransaction? transaction, CancellationToken cancellationToken)
    {
        foreach (var statement in statements)
            await _executor.ExecuteAsync(statement, transaction: transaction, cancellationToken: cancellationToken);
    }
}
