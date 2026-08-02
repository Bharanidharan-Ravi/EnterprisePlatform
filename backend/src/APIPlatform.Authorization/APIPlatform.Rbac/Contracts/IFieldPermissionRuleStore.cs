using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

public interface IFieldPermissionRuleStore
{
    Task<IReadOnlyCollection<FieldPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default);
    Task AddRuleAsync(string tenantId, FieldPermissionRule rule, CancellationToken cancellationToken = default);
}
