using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>
/// Extension point: consuming apps register named policy delegates here. Rbac never contains
/// domain policy logic itself (Hard Rule: no business logic in Nucleus packages).
/// </summary>
public interface IPolicyRuleRegistry
{
    void Register(string name, Func<AuthorizationContext, CancellationToken, Task<bool>> rule);
    bool TryResolve(string name, out Func<AuthorizationContext, CancellationToken, Task<bool>>? rule);
}
