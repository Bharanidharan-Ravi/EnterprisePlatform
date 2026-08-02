using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Results;

namespace APIPlatform.CrudEngine.Validation;

/// <summary>Default IValidationPipeline — walks CrudContext.EntityDefinition.Fields, reads each
/// property's value off CrudContext.Entity via reflection, and evaluates rules through
/// ValidationRuleEvaluator. Skipped entirely for read operations (GetByKey/List).</summary>
public sealed class MetadataValidationPipeline : IValidationPipeline
{
    public ValidationResult Validate<TEntity>(CrudContext<TEntity> context) where TEntity : class
    {
        if (context.Operation is CrudOperationType.GetByKey or CrudOperationType.List || context.Entity is null || context.EntityDefinition is null)
            return ValidationResult.Success();

        var errors = new List<string>();
        var entityType = typeof(TEntity);

        foreach (var field in context.EntityDefinition.Fields)
        {
            var prop = entityType.GetProperty(field.Name);
            var value = prop?.GetValue(context.Entity);
            errors.AddRange(ValidationRuleEvaluator.Evaluate(field, value));
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.Select(e => new ErrorInfo { Code = "validation_failed", Message = e }).ToList());
    }
}
