using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Validation;

/// <summary>
/// Evaluates a single field's metadata-driven validation rules (Nucleus.SharedSchema.Models
/// FieldDefinition.Validation, a singular nullable ValidationRuleDefinition) against a raw
/// value. Phase 2 fix: this previously read via reflection for "IsRequired"/"ValidationRules"
/// properties that do not exist on the real (frozen) FieldDefinition shape — that assumption
/// boundary predated the real SharedSchema being wired in, and silently disabled all
/// metadata validation (isRequired always false, rules always null -&gt; yield break). Reading
/// the real FieldDefinition.Validation properties directly closes that gap.
/// </summary>
public static class ValidationRuleEvaluator
{
    public static IEnumerable<string> Evaluate(FieldDefinition field, object? value)
    {
        var rule = field.Validation;
        if (rule is null) yield break;

        var isEmpty = value is null || (value is string s && string.IsNullOrWhiteSpace(s));

        if (rule.Required && isEmpty)
        {
            yield return $"{field.Name} is required.";
            // No further checks are meaningful against a missing/empty value.
            yield break;
        }

        if (isEmpty) yield break;

        if (value is string str)
        {
            if (rule.MinLength is int minLength && str.Length < minLength)
                yield return $"{field.Name} must be at least {minLength} characters.";

            if (rule.MaxLength is int maxLength && str.Length > maxLength)
                yield return $"{field.Name} exceeds max length of {maxLength}.";

            if (!string.IsNullOrEmpty(rule.RegexPattern) &&
                !System.Text.RegularExpressions.Regex.IsMatch(str, rule.RegexPattern))
                yield return $"{field.Name} does not match required format.";
        }

        if ((rule.MinValue is not null || rule.MaxValue is not null) && TryToDecimal(value, out var numeric))
        {
            if (rule.MinValue is decimal minValue && numeric < minValue)
                yield return $"{field.Name} is below minimum.";

            if (rule.MaxValue is decimal maxValue && numeric > maxValue)
                yield return $"{field.Name} exceeds maximum.";
        }
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        try
        {
            result = Convert.ToDecimal(value);
            return true;
        }
        catch
        {
            result = 0m;
            return false;
        }
    }
}
