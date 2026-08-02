namespace APIPlatform.Rbac.Models;

/// <summary>
/// A generic menu node for Menu Authorization. Nucleus.SharedSchema does not yet define a
/// menu/nav schema section (only entity schema per Section 6.1) — per the prior architecture
/// review this is deliberately NOT extended here; MenuItem trees are supplied by the
/// consuming app/UI registry, Rbac only filters them.
/// </summary>
public sealed record MenuItem
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? RequiredPermissionKey { get; init; }
    public IReadOnlyList<MenuItem> Children { get; init; } = Array.Empty<MenuItem>();
}
