namespace APIPlatform.CrudEngine.Adapters;

/// <summary>
/// ASSUMPTION BOUNDARY: I don't have APIPlatform.Data's actual IDatabaseExecutor /
/// IStoredProcedureExecutor / IMultiResultReader source, so this port declares the minimal
/// stored-procedure execution surface CrudEngine needs and isolates the assumption to one
/// adapter implementation (DatabaseExecutorProcedurePort, in the DI project or a small
/// Infrastructure shim). If the real signatures differ, only that one adapter class changes —
/// nothing else in CrudEngine references APIPlatform.Data's SP types directly.
/// </summary>
public interface IProcedurePort
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);

    /// <summary>One SP call, N result sets, each read as a distinct CLR type in declared order.</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<object>>> QueryMultipleAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyList<(string ResultKey, Type EntityType)> resultSets,
        CancellationToken cancellationToken = default);
}
