using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Policy;

/// <summary>
/// POLICY EVALUATION responsibility only: given a PolicyRule that PermissionResolver/
/// PlanningStage has already determined applies, invokes its named delegate and returns
/// pass/fail. This class never decides WHICH policies apply and never touches grant data
/// (AllowedKeys/DeniedKeys) — that is Permission Resolution's job. Policy Evaluation answers
/// exactly one question: "does this already-selected rule pass, right now, for this context?"
/// Fail-closed: a PolicyRule whose Name isn't registered is treated as a DENIAL, never
/// silently ignored or treated as pass — an unregistered policy is a configuration error, and
/// the safe failure mode for an authorization engine is to deny, not to allow.
/// </summary>
public sealed class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRuleRegistry _registry;

    public PolicyEngine(IPolicyRuleRegistry registry) => _registry = registry;

    public async Task<bool> EvaluateAsync(PolicyRule rule, AuthorizationContext context, CancellationToken cancellationToken = default)
    {
        if (!_registry.TryResolve(rule.Name, out var handler) || handler is null)
            return false;

        return await handler(context, cancellationToken);
    }
}
