using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Validation;

/// <summary>
/// ASSUMPTION BOUNDARY: I don't have the exact ValidationRuleDefinition/FieldDefinition property
/// shapes from your frozen SharedSchema source, so evaluation against a single field's raw value
/// is isolated here. Adjust only this file if the real property names differ from what's read
/// via reflection below (RuleType/Pattern/Min/Max/AllowedValues/IsRequired conventions).
/// </summary>
public static class ValidationRuleEvaluator
{
    public static IEnumerable<string> Evaluate(FieldDefinition field, object? value)
    {
        var fieldType = field.GetType();
        var isRequired = fieldType.GetProperty("IsRequired")?.GetValue(field) as bool? ?? false;

        if (isRequired && (value is null || (value is string s && string.IsNullOrWhiteSpace(s))))
            yield return $"{field.Name} is required.";

        var rules = fieldType.GetProperty("ValidationRules")?.GetValue(field) as System.Collections.IEnumerable;
        if (rules is null) yield break;

        foreach (var rule in rules)
        {
            var ruleType = rule.GetType();
            var kind = ruleType.GetProperty("RuleType")?.GetValue(rule)?.ToString() ?? string.Empty;

            switch (kind)
            {
                case "MaxLength" when value is string str:
                    var max = ruleType.GetProperty("Max")?.GetValue(rule) as int?;
                    if (max is not null && str.Length > max)
                        yield return $"{field.Name} exceeds max length of {max}.";
                    break;

                case "Range" when value is IComparable comparable:
                    var min = ruleType.GetProperty("Min")?.GetValue(rule);
                    var rangeMax = ruleType.GetProperty("Max")?.GetValue(rule);
                    if (min is IComparable minC && comparable.CompareTo(minC) < 0)
                        yield return $"{field.Name} is below minimum.";
                    if (rangeMax is IComparable maxC && comparable.CompareTo(maxC) > 0)
                        yield return $"{field.Name} exceeds maximum.";
                    break;

                case "Regex" when value is string str2:
                    var pattern = ruleType.GetProperty("Pattern")?.GetValue(rule) as string;
                    if (!string.IsNullOrEmpty(pattern) && !System.Text.RegularExpressions.Regex.IsMatch(str2, pattern))
                        yield return $"{field.Name} does not match required format.";
                    break;

                case "Enum":
                    var allowed = ruleType.GetProperty("AllowedValues")?.GetValue(rule) as System.Collections.IEnumerable;
                    if (allowed is not null && value is not null && !allowed.Cast<object>().Any(a => Equals(a, value)))
                        yield return $"{field.Name} is not a valid value.";
                    break;
            }
        }
    }
}
