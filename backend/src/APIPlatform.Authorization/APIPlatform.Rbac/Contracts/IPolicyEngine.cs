using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

public interface IPolicyEngine
{
    Task<bool> EvaluateAsync(PolicyRule rule, AuthorizationContext context, CancellationToken cancellationToken = default);
}
