namespace Nucleus.SharedSchema.Models;
public sealed record PermissionRequirement
{
    public IReadOnlyCollection<string> ReadRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> WriteRoles { get; init; } = Array.Empty<string>();
}
