using Nucleus.SharedSchema.Enums;
namespace Nucleus.SharedSchema.Models;
public sealed record UiHintDefinition
{
    public required UiInputType InputType { get; init; }
    public required string DisplayLabel { get; init; }
    public int? ColumnWidth { get; init; }
    public bool Visible { get; init; } = true;
    public string? VisibilityRuleName { get; init; }
}
