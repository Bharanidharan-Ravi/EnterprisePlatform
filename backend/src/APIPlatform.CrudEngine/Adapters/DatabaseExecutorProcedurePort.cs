using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using APIPlatform.CrudEngine.Services;
using APIPlatform.Data.Execution;

namespace APIPlatform.CrudEngine.Adapters;

/// <summary>
/// Default IProcedurePort — resolves the ASSUMPTION BOUNDARY flagged on IProcedurePort by
/// executing stored procedures through APIPlatform.Data's IDatabaseExecutor (the same
/// abstraction GenericRepository's SQL path sits on), with CommandType.StoredProcedure.
/// AddCrudEngine() registers this so consuming apps get a working IProcedurePort with zero
/// extra wiring; an app can still override the registration with its own adapter.
/// </summary>
public sealed class DatabaseExecutorProcedurePort : IProcedurePort
{
    private readonly IDatabaseExecutor _executor;

    public DatabaseExecutorProcedurePort(IDatabaseExecutor executor) => _executor = executor;

    public Task<IReadOnlyList<T>> QueryAsync<T>(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default) =>
        _executor.QueryAsync<T>(procedureName, parameters, CommandType.StoredProcedure, cancellationToken: cancellationToken);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default) =>
        _executor.QuerySingleOrDefaultAsync<T>(procedureName, parameters, CommandType.StoredProcedure, cancellationToken: cancellationToken);

    public Task<int> ExecuteAsync(string procedureName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default) =>
        _executor.ExecuteAsync(procedureName, parameters, CommandType.StoredProcedure, cancellationToken: cancellationToken);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<object>>> QueryMultipleAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyList<(string ResultKey, Type EntityType)> resultSets,
        CancellationToken cancellationToken = default)
    {
        var reader = await _executor.QueryMultipleAsync(procedureName, parameters, CommandType.StoredProcedure, cancellationToken: cancellationToken);
        await using var _ = reader.ConfigureAwait(false);

        var results = new Dictionary<string, IReadOnlyList<object>>();
        foreach (var (resultKey, entityType) in resultSets)
        {
            // IMultiResultReader.ReadAsync<T>() is generic; EntityType is only known at runtime,
            // so invoke it via a cached MethodInfo (mirrors CompiledInvokerCache's approach) instead
            // of a fresh MakeGenericMethod lookup per call.
            var task = (Task)GetReadAsyncMethod(entityType).Invoke(reader, null)!;
            await task.ConfigureAwait(false);
            var rows = (IEnumerable)ResultPropertyCache.GetResult(task)!;
            results[resultKey] = rows.Cast<object>().ToList();
        }

        return results;
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> ReadAsyncMethods = new();

    private static MethodInfo GetReadAsyncMethod(Type entityType) =>
        ReadAsyncMethods.GetOrAdd(entityType, t =>
            typeof(IMultiResultReader).GetMethod(nameof(IMultiResultReader.ReadAsync))!.MakeGenericMethod(t));
}
