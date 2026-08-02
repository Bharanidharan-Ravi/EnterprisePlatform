using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

public interface IRowPermissionRuleStore
{
    Task<IReadOnlyCollection<RowPermissionRule>> GetRulesAsync(string tenantId, string entityKey, CancellationToken cancellationToken = default);
    Task AddRuleAsync(string tenantId, RowPermissionRule rule, CancellationToken cancellationToken = default);
}
