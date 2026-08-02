using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Results;

namespace APIPlatform.CrudEngine.Validation;

/// <summary>Metadata-driven validation (Req 7) — evaluates FieldDefinition.ValidationRuleDefinition
/// entries from SharedSchema. No business validation is ever added here; only generic rule types
/// (Required/Length/Range/Regex/Type/Enum).</summary>
public interface IValidationPipeline
{
    ValidationResult Validate<TEntity>(CrudContext<TEntity> context) where TEntity : class;
}
