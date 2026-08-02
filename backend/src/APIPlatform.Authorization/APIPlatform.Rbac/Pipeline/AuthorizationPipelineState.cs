using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Pipeline;

/// <summary>
/// Mutable state object threaded through all six stages of one evaluation. Not shared across
/// requests — a new instance is created per call to IPermissionEvaluator.EvaluateAsync.
/// </summary>
public sealed class AuthorizationPipelineState
{
    public required AuthorizationRequest Request { get; init; }

    public AuthorizationContext? Context { get; set; }
    public IReadOnlyCollection<string> RequiredPermissionKeys { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<PolicyRule> ApplicablePolicies { get; set; } = Array.Empty<PolicyRule>();

    public bool? Decision { get; set; }
    public string? DenialReason { get; set; }
    public RowFilterDescriptor? RowFilter { get; set; }
    public FieldMaskDescriptor? FieldMask { get; set; }

    public AuthorizationResult? Result { get; set; }
}
