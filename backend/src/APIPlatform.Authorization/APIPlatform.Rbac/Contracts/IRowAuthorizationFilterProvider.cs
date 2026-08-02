using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

public interface IRowAuthorizationFilterProvider
{
    Task<RowFilterDescriptor> GetRowFilterAsync(string entityKey, CancellationToken cancellationToken = default);
}
