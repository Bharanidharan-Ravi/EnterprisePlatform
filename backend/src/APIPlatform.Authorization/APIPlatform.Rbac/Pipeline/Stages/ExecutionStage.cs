using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 5: Execution. The only stage that actually performs the authorization decision, as
/// two explicitly separate checks:
///   1. Permission check (EvaluatePermissionGrants) — static grant lookup, deny-overrides-allow.
///   2. Policy check (EvaluatePoliciesAsync) — delegates to IPolicyEngine per applicable rule.
/// Both must pass (fail-closed). Also computes FieldMask/RowFilter outputs when relevant.
/// </summary>
public sealed class ExecutionStage : IAuthorizationStage
{
    private readonly IPolicyEngine _policyEngine;
    private readonly IFieldPermissionRuleStore _fieldRuleStore;
    private readonly IRowPermissionRuleStore _rowRuleStore;
    private readonly IRowFilterRegistry _rowFilterRegistry;

    public ExecutionStage(
        IPolicyEngine policyEngine,
        IFieldPermissionRuleStore fieldRuleStore,
        IRowPermissionRuleStore rowRuleStore,
        IRowFilterRegistry rowFilterRegistry)
    {
        _policyEngine = policyEngine;
        _fieldRuleStore = fieldRuleStore;
        _rowRuleStore = rowRuleStore;
        _rowFilterRegistry = rowFilterRegistry;
    }

    public async Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var context = state.Context!;
        var permissions = context.EffectivePermissions!;

        var grantAllowed = EvaluatePermissionGrants(state.RequiredPermissionKeys, permissions);
        var policyAllowed = await EvaluatePoliciesAsync(state.ApplicablePolicies, context, cancellationToken);

        state.Decision = grantAllowed && policyAllowed;
        state.DenialReason = state.Decision == true
            ? null
            : !grantAllowed
                ? "No matching permission grant for the required permission key(s)."
                : "One or more applicable policy rules denied access.";

        if (state.Decision != true)
            return;

        if (state.Request.ResourceType == ResourceType.Field)
        {
            var rules = await _fieldRuleStore.GetRulesAsync(context.TenantId, state.Request.ResourceKey, cancellationToken);
            state.FieldMask = FieldMaskDescriptor.FromRules(rules, permissions);
        }

        if (state.Request.ResourceType == ResourceType.Row)
        {
            var rules = await _rowRuleStore.GetRulesAsync(context.TenantId, state.Request.ResourceKey, cancellationToken);
            var applicableRule = rules.FirstOrDefault();

            if (applicableRule is not null && _rowFilterRegistry.TryResolve(applicableRule.FilterDelegateKey, out var filterBuilder) && filterBuilder is not null)
            {
                state.RowFilter = await filterBuilder(context);
            }
        }
    }

    /// <summary>
    /// PERMISSION check: pure static grant lookup against the already-resolved PermissionSet.
    /// No delegate invocation, no I/O — this is what "Permission Resolution" produced upstream,
    /// evaluated here. Deny always overrides Allow (enforced by PermissionResolver already
    /// excluding denied keys from AllowedKeys, but re-checked defensively here too).
    /// </summary>
    private static bool EvaluatePermissionGrants(IReadOnlyCollection<string> requiredKeys, PermissionSet permissions) =>
        requiredKeys.All(k => permissions.AllowedKeys.Contains(k)) &&
        !requiredKeys.Any(k => permissions.DeniedKeys.Contains(k));

    /// <summary>
    /// POLICY check: delegates each applicable rule to IPolicyEngine (see PolicyEngine.cs for
    /// the fail-closed contract). This method never reads AllowedKeys/DeniedKeys directly —
    /// that separation is the point: permission grants and policy outcomes are independently
    /// testable and independently cacheable.
    /// </summary>
    private async Task<bool> EvaluatePoliciesAsync(
        IReadOnlyCollection<Models.PolicyRule> applicablePolicies,
        Contexts.AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        foreach (var policy in applicablePolicies)
        {
            if (!await _policyEngine.EvaluateAsync(policy, context, cancellationToken))
                return false;
        }
        return true;
    }
}
