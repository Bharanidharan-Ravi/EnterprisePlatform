using APIPlatform.Rbac.Common;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Rbac.Policy;

public sealed class PolicyRuleRegistry
    : NamedDelegateRegistry<Func<AuthorizationContext, CancellationToken, Task<bool>>>, IPolicyRuleRegistry
{
}
