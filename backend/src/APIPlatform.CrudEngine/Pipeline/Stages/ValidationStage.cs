using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Validation;

namespace APIPlatform.CrudEngine.Pipeline.Stages;

/// <summary>Stage 3 — determines whether execution may continue. Never generates SQL, never
/// touches the database. Sets ShortCircuited+Error on failure so CrudPipeline stops early.</summary>
public sealed class ValidationStage<TEntity> : IPipelineStage<TEntity> where TEntity : class
{
    private readonly IValidationPipeline _validation;

    public ValidationStage(IValidationPipeline validation) => _validation = validation;

    public Task ExecuteAsync(CrudContext<TEntity> context)
    {
        context.ValidationResult = _validation.Validate(context);
        if (context.ValidationResult?.IsValid == false)
        {
            context.ShortCircuited = true;
            context.Error = context.ValidationResult.Errors.FirstOrDefault();
        }
        return Task.CompletedTask;
    }
}
