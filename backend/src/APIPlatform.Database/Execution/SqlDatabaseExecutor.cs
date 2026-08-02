using System.Data;
using System.Diagnostics;
using Dapper;
using APIPlatform.Data.Connections;
using APIPlatform.Data.Diagnostics;
using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Options;
using APIPlatform.Data.Providers;
using APIPlatform.Data.Resilience;
using APIPlatform.Data.Transactions;
using APIPlatform.Foundation.Exceptions;
using Microsoft.Extensions.Options;

namespace APIPlatform.Data.Execution;

/// <summary>
/// Default IDatabaseExecutor. Dapper is an implementation detail confined to this class —
/// no other type in the package (or any consumer) references Dapper types directly.
/// Every command runs through the registered IDatabaseRetryPolicy (a no-op by default) and
/// is observed by any registered DatabaseDiagnosticsListener instances (none by default),
/// giving future resilience and observability packages a hook without Database depending on either.
/// </summary>
public sealed class SqlDatabaseExecutor : IDatabaseExecutor
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly DatabaseOptions _options;
    private readonly IDatabaseRetryPolicy _retryPolicy;
    private readonly IReadOnlyList<DatabaseDiagnosticsListener> _listeners;

    public SqlDatabaseExecutor(
        IDatabaseConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> options,
        IDatabaseRetryPolicy retryPolicy,
        IEnumerable<DatabaseDiagnosticsListener> listeners)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _retryPolicy = retryPolicy;
        _listeners = listeners.ToList();
    }

    public Task<int> ExecuteAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        RunInstrumentedAsync(sql, commandType, transaction, async (connection, dbTransaction) =>
        {
            var command = BuildCommand(sql, parameters, commandType, dbTransaction, commandTimeoutSeconds, cancellationToken);
            return await connection.ExecuteAsync(command);
        });

    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        RunInstrumentedAsync(sql, commandType, transaction, async (connection, dbTransaction) =>
        {
            var command = BuildCommand(sql, parameters, commandType, dbTransaction, commandTimeoutSeconds, cancellationToken);
            var result = await connection.QueryAsync<T>(command);
            return (IReadOnlyList<T>)result.AsList();
        });

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        RunInstrumentedAsync(sql, commandType, transaction, async (connection, dbTransaction) =>
        {
            var command = BuildCommand(sql, parameters, commandType, dbTransaction, commandTimeoutSeconds, cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command);
        });

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        RunInstrumentedAsync(sql, commandType, transaction, async (connection, dbTransaction) =>
        {
            var command = BuildCommand(sql, parameters, commandType, dbTransaction, commandTimeoutSeconds, cancellationToken);
            return await connection.QueryFirstOrDefaultAsync<T>(command);
        });

    public Task<T?> ExecuteScalarAsync<T>(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        RunInstrumentedAsync(sql, commandType, transaction, async (connection, dbTransaction) =>
        {
            var command = BuildCommand(sql, parameters, commandType, dbTransaction, commandTimeoutSeconds, cancellationToken);
            return await connection.ExecuteScalarAsync<T>(command);
        });

    public async Task<IMultiResultReader> QueryMultipleAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text, IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        NotifyExecuting(sql, commandType);

        // Multi-result queries always own their own connection lifetime (via the reader), even when
        // run inside an existing transaction's connection, so the caller controls disposal explicitly.
        IDbConnection connection = transaction is DatabaseTransaction dt
            ? dt.Connection
            : await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            var command = BuildCommand(sql, parameters, commandType,
                transaction is DatabaseTransaction t ? t.Transaction : null, commandTimeoutSeconds, cancellationToken);
            var gridReader = await connection.QueryMultipleAsync(command);
            var owner = transaction is DatabaseTransaction ? new NoOpDisposable() : connection;
            NotifyExecuted(sql, commandType, sw.Elapsed);
            return new MultiResultReader(gridReader, owner);
        }
        catch (Exception ex)
        {
            if (transaction is not DatabaseTransaction) connection.Dispose();
            NotifyFailed(sql, commandType, ex, sw.Elapsed);
            throw new DatabaseException("QueryMultipleAsync failed.", ex, ErrorCategory.Infrastructure);
        }
    }

    public async Task<IDatabaseTransaction> BeginTransactionAsync(IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        try
        {
            var transaction = connection.BeginTransaction(isolationLevel ?? _options.DefaultIsolationLevel);
            return new DatabaseTransaction(connection, transaction);
        }
        catch (Exception ex)
        {
            connection.Dispose();
            throw new DatabaseException("Failed to begin transaction.", ex, ErrorCategory.Infrastructure);
        }
    }

    private async Task<T> RunInstrumentedAsync<T>(string sql, CommandType commandType, IDatabaseTransaction? transaction,
        Func<IDbConnection, IDbTransaction?, Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        NotifyExecuting(sql, commandType);
        try
        {
            var result = await _retryPolicy.ExecuteAsync(() => RunCoreAsync(transaction, action));
            NotifyExecuted(sql, commandType, sw.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            NotifyFailed(sql, commandType, ex, sw.Elapsed);
            throw;
        }
    }

    private async Task<T> RunCoreAsync<T>(IDatabaseTransaction? transaction, Func<IDbConnection, IDbTransaction?, Task<T>> action)
    {
        if (transaction is DatabaseTransaction dt)
        {
            try { return await action(dt.Connection, dt.Transaction); }
            catch (DatabaseException) { throw; }
            catch (Exception ex) { throw new DatabaseException("Command execution failed.", ex, ErrorCategory.Infrastructure); }
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        try { return await action(connection, null); }
        catch (DatabaseException) { throw; }
        catch (Exception ex) { throw new DatabaseException("Command execution failed.", ex, ErrorCategory.Infrastructure); }
    }

    private CommandDefinition BuildCommand(string sql, IReadOnlyDictionary<string, object?>? parameters,
        CommandType commandType, IDbTransaction? transaction, int? commandTimeoutSeconds, CancellationToken cancellationToken)
    {
        DynamicParameters? dynamicParameters = null;
        if (parameters is not null)
        {
            dynamicParameters = new DynamicParameters();
            foreach (var (key, value) in parameters) dynamicParameters.Add(key, value);
        }

        return new CommandDefinition(
            sql,
            dynamicParameters,
            transaction: transaction,
            commandTimeout: commandTimeoutSeconds ?? _options.CommandTimeoutSeconds,
            commandType: commandType,
            cancellationToken: cancellationToken);
    }

    private void NotifyExecuting(string sql, CommandType commandType)
    {
        foreach (var listener in _listeners) listener.OnCommandExecuting(sql, commandType);
    }

    private void NotifyExecuted(string sql, CommandType commandType, TimeSpan duration)
    {
        foreach (var listener in _listeners) listener.OnCommandExecuted(sql, commandType, duration);
    }

    private void NotifyFailed(string sql, CommandType commandType, Exception ex, TimeSpan duration)
    {
        foreach (var listener in _listeners) listener.OnCommandFailed(sql, commandType, ex, duration);
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
