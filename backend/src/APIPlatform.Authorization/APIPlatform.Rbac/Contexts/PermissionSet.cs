using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contexts;

/// <summary>
/// The resolved, cacheable set of effective permissions for one (tenant, user) pair.
/// This is what IPermissionCache stores — never the per-request AuthorizationResult, which is
/// contextual and not safely cacheable across different resource instances.
/// </summary>
public sealed class PermissionSet
{
    public required string TenantId { get; init; }
    public required string UserId { get; init; }
    public IReadOnlySet<string> AllowedKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> DeniedKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<PolicyRule> PolicyRules { get; init; } = Array.Empty<PolicyRule>();
}
