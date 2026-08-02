using System.Data;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;

namespace APIPlatform.Data.StoredProcedures;

/// <summary>Default IStoredProcedureExecutor — delegates to IDatabaseExecutor with CommandType.StoredProcedure.</summary>
public sealed class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IDatabaseExecutor _executor;

    public StoredProcedureExecutor(IDatabaseExecutor executor) => _executor = executor;

    public Task<int> ExecuteProcedureAsync(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        _executor.ExecuteAsync(procedureName, parameters, CommandType.StoredProcedure, transaction, commandTimeoutSeconds, cancellationToken);

    public Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        _executor.QueryAsync<T>(procedureName, parameters, CommandType.StoredProcedure, transaction, commandTimeoutSeconds, cancellationToken);

    public Task<IMultiResultReader> QueryMultipleProcedureAsync(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        _executor.QueryMultipleAsync(procedureName, parameters, CommandType.StoredProcedure, transaction, commandTimeoutSeconds, cancellationToken);

    public Task<T?> ScalarProcedureAsync<T>(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default) =>
        _executor.ExecuteScalarAsync<T>(procedureName, parameters, CommandType.StoredProcedure, transaction, commandTimeoutSeconds, cancellationToken);
}
