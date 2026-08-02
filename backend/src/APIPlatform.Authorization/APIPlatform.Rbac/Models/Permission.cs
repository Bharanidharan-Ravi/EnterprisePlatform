namespace APIPlatform.Rbac.Models;

/// <summary>
/// A permission definition. Key is a free-form, data-driven string (e.g. "Widget.Read",
/// "Menu.Dashboard.View") — deliberately NOT an enum, because one Rbac package must serve many
/// generated apps with different permission sets (Master Plan Section 9.7).
/// </summary>
public sealed class Permission
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required ResourceType ResourceType { get; init; }
}
