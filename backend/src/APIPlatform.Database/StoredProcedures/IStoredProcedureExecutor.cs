using APIPlatform.Data.Transactions;

namespace APIPlatform.Data.StoredProcedures;

/// <summary>
/// First-class stored procedure execution — so consuming applications never re-implement
/// CommandType.StoredProcedure plumbing themselves.
/// </summary>
public interface IStoredProcedureExecutor
{
    Task<int> ExecuteProcedureAsync(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);

    Task<Execution.IMultiResultReader> QueryMultipleProcedureAsync(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);

    Task<T?> ScalarProcedureAsync<T>(string procedureName, IReadOnlyDictionary<string, object?>? parameters = null,
        IDatabaseTransaction? transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);
}
