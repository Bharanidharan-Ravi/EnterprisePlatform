using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contexts;

/// <summary>
/// The input to IPermissionEvaluator.EvaluateAsync — one shape for every resource type
/// (Api/Crud/Field/Row/Menu/Feature/Policy), per the "single pipeline, thin facades" design.
/// </summary>
public sealed class AuthorizationRequest
{
    public required ResourceType ResourceType { get; init; }

    /// <summary>Entity key, route key, menu key, or feature key depending on ResourceType.</summary>
    public required string ResourceKey { get; init; }

    /// <summary>e.g. "Read", "Write", "Execute", "View", "Use".</summary>
    public required string Action { get; init; }

    /// <summary>Required when ResourceType is Field.</summary>
    public string? FieldKey { get; init; }

    /// <summary>
    /// Explicit permission key to check, bypassing the default "{ResourceKey}.{Action}"
    /// derivation. Menu Authorization uses this since menu permission keys are supplied
    /// directly by the consuming app's menu config, not derived from the menu key.
    /// </summary>
    public string? PermissionKeyOverride { get; init; }
}
