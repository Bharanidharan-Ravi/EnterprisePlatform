using System.Data;
using APIPlatform.Data.Transactions;

namespace APIPlatform.Data.Execution;

/// <summary>
/// Core SQL execution abstraction. All Dapper-specific types (DynamicParameters, SqlMapper,
/// GridReader) are hidden behind this contract — consumers pass plain parameter dictionaries
/// and receive plain CLR results.
/// </summary>
public interface IDatabaseExecutor
{
    Task<int> ExecuteAsync(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    Task<IMultiResultReader> QueryMultipleAsync(
        string sql,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.Text,
        IDatabaseTransaction? transaction = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Begins a new transaction on a dedicated connection. Pass the result into other calls' `transaction` parameter to run within it.</summary>
    Task<IDatabaseTransaction> BeginTransactionAsync(
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default);
}
