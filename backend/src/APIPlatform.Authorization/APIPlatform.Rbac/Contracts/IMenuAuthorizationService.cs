using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contracts;

public interface IMenuAuthorizationService
{
    Task<IReadOnlyList<MenuItem>> FilterMenuAsync(IReadOnlyList<MenuItem> menu, CancellationToken cancellationToken = default);
}
