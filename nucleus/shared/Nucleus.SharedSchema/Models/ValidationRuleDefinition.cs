namespace Nucleus.SharedSchema.Models;
public sealed record ValidationRuleDefinition
{
    public bool Required { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public string? RegexPattern { get; init; }
    public string? CrossFieldRuleName { get; init; }
}
