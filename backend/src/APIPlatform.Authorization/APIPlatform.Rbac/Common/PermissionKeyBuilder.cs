using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Common;

/// <summary>Builds the default "{ResourceKey}.{Action}" permission key shape.</summary>
public static class PermissionKeyBuilder
{
    public static string Build(ResourceType resourceType, string resourceKey, string action) =>
        $"{resourceKey}.{action}";
}
