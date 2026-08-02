using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Default IBatchCrudExecutor. Resolves IRepository&lt;TEntity&gt; per unit via
/// IEntityTypeRegistry + reflection (TEntity isn't known at compile time here — same technique
/// SyncRepositoryV2 used with MakeGenericMethod, applied generically instead of per-app).
/// </summary>
public sealed class BatchCrudExecutor : IBatchCrudExecutor
{
    private readonly IServiceProvider _services;
    private readonly IEntityTypeRegistry _typeRegistry;

    public BatchCrudExecutor(IServiceProvider services, IEntityTypeRegistry typeRegistry)
    {
        _services = services;
        _typeRegistry = typeRegistry;
    }

    public async Task<CrudBatchResponse> ExecuteAsync(IReadOnlyList<CrudBatchUnit> units, CancellationToken cancellationToken = default)
    {
        var response = new CrudBatchResponse();

        var tasks = units.Select(async unit =>
        {
            try
            {
                if (!_typeRegistry.TryResolve(unit.EntityName, out var entityType))
                    return (unit.ResultKey, Fail("UNKNOWN_ENTITY", $"No CLR type registered for entity '{unit.EntityName}'."));

                var repoType = typeof(IRepository<>).MakeGenericType(entityType);
                var repo = _services.GetService(repoType)
                    ?? throw new InvalidOperationException($"IRepository<{entityType.Name}> is not registered.");

                var result = await InvokeAsync(repo, repoType, unit, cancellationToken);
                return (unit.ResultKey, result);
            }
            catch (Exception ex)
            {
                return (unit.ResultKey, Fail("BATCH_UNIT_ERROR", ex.Message));
            }
        }).ToList();

        var completed = await Task.WhenAll(tasks);
        foreach (var (key, result) in completed)
            response.Results[key] = result;

        return response;
    }

    private static async Task<CrudBatchResult> InvokeAsync(object repo, Type repoType, CrudBatchUnit unit, CancellationToken ct)
    {
        return unit.Operation switch
        {
            CrudOperationType.List => Ok(await InvokeMethod(repo, repoType, "ListAsync", new object?[] { ct })),

            CrudOperationType.GetByKey => Ok(await InvokeMethod(repo, repoType, "GetByKeyAsync", new object?[] { ToEntityKey(unit.Key), ct })),

            CrudOperationType.Create => await InvokeMutation(repo, repoType, "AddAsync", unit.Payload, ct),

            CrudOperationType.Update => await InvokeMutation(repo, repoType, "UpdateAsync", unit.Payload, ct),

            CrudOperationType.Delete => await InvokeMutation(repo, repoType, "DeleteAsync", ToEntityKey(unit.Key), ct),

            _ => Fail("UNSUPPORTED_OPERATION", $"Operation '{unit.Operation}' is not supported.")
        };
    }

    private static async Task<object?> InvokeMethod(object repo, Type repoType, string methodName, object?[] args)
    {
        var invoker = CompiledInvokerCache.GetInvoker(repoType, methodName);
        var task = (Task)invoker(repo, args)!;
        await task;
        return ResultPropertyCache.GetResult(task);
    }

    private static async Task<CrudBatchResult> InvokeMutation(object repo, Type repoType, string methodName, object? payload, CancellationToken ct)
    {
        var invoker = CompiledInvokerCache.GetInvoker(repoType, methodName);
        var task = (Task)invoker(repo, new[] { payload, (object?)ct })!;
        await task;
        var outcome = ResultPropertyCache.GetResult(task);

        // OperationResult / Result<T> both expose IsSuccess + Error per Foundation.Results — read
        // via cached property getters (Req 13) so BatchCrudExecutor stays independent of which of
        // the two was returned without paying repeated GetProperty/GetValue reflection cost.
        if (outcome is null) return Fail("MUTATION_FAILED", "Operation returned no result.");

        var isSuccess = (bool)(PropertyAccessorCache.Get(outcome.GetType(), "IsSuccess")?.Invoke(outcome) ?? false);
        if (isSuccess) return Ok(outcome);

        var error = PropertyAccessorCache.Get(outcome.GetType(), "Error")?.Invoke(outcome);
        var code = error is null ? "MUTATION_FAILED" : PropertyAccessorCache.Get(error.GetType(), "Code")?.Invoke(error) as string ?? "MUTATION_FAILED";
        var message = error is null ? "Operation failed." : PropertyAccessorCache.Get(error.GetType(), "Message")?.Invoke(error) as string ?? "Operation failed.";
        return Fail(code, message);
    }

    private static EntityKey ToEntityKey(EntityKeyValues? key) =>
        // ASSUMPTION: EntityKey exposes a public constructor accepting IDictionary<string, object?>
        // (context summary: "EntityKey implements IReadOnlyDictionary, case-insensitive"). If the
        // real constructor/factory differs, only this one line needs to change.
        new EntityKey(key ?? new EntityKeyValues());

    private static CrudBatchResult Ok(object? data) => new() { Ok = true, Data = data };
    private static CrudBatchResult Fail(string code, string message) => new() { Ok = false, ErrorCode = code, ErrorMessage = message };
}
