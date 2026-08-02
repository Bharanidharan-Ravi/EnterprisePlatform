using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contexts;

/// <summary>The output of IPermissionEvaluator.EvaluateAsync — the Response of the pipeline.</summary>
public sealed class AuthorizationResult
{
    public required bool Allowed { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyCollection<PolicyRule> AppliedPolicies { get; init; } = Array.Empty<PolicyRule>();
    public RowFilterDescriptor? RowFilter { get; init; }
    public FieldMaskDescriptor? FieldMask { get; init; }
}
