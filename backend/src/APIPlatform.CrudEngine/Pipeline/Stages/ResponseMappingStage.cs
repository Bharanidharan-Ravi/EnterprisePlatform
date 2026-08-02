using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Services;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>
/// Stage 6 — transforms the execution result (or a short-circuited validation failure) into a
/// uniform CrudResponse. Only responsible for response construction; does not touch metadata,
/// validation, planning, or execution. Reuses PropertyAccessorCache (already introduced for
/// BatchCrudExecutor) to read IsSuccess/Error off whichever Result&lt;TEntity&gt;/OperationResult
/// the execution stage produced, without knowing the concrete type.
/// </summary>
public sealed class ResponseMappingStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        if (context.ShortCircuited)
        {
            context.Response = new CrudResponse<TEntity>
            {
                Ok = false,
                ErrorCode = context.Error?.Code,
                ErrorMessage = context.Error?.Message
            };
            return Task.CompletedTask;
        }

        var outcome = context.ExecutionResult;
        var isSuccessGetter = outcome is null ? null : PropertyAccessorCache.Get(outcome.GetType(), "IsSuccess");

        if (isSuccessGetter is null)
        {
            // Plain data result (TEntity?, IReadOnlyList<TEntity>) — GetByKey/List.
            context.Response = new CrudResponse<TEntity> { Ok = true, Data = outcome };
            return Task.CompletedTask;
        }

        var isSuccess = (bool)(isSuccessGetter(outcome!) ?? false);
        if (isSuccess)
        {
            context.Response = new CrudResponse<TEntity> { Ok = true, Data = outcome };
            return Task.CompletedTask;
        }

        var error = PropertyAccessorCache.Get(outcome!.GetType(), "Error")?.Invoke(outcome);
        var code = error is null ? "OPERATION_FAILED" : PropertyAccessorCache.Get(error.GetType(), "Code")?.Invoke(error) as string ?? "OPERATION_FAILED";
        var message = error is null ? "Operation failed." : PropertyAccessorCache.Get(error.GetType(), "Message")?.Invoke(error) as string ?? "Operation failed.";
        context.Response = new CrudResponse<TEntity> { Ok = false, ErrorCode = code, ErrorMessage = message };
        return Task.CompletedTask;
    }
}
