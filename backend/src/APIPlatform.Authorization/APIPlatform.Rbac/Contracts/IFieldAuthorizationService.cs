using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

public interface IFieldAuthorizationService
{
    Task<FieldMaskDescriptor> GetFieldMaskAsync(string entityKey, CancellationToken cancellationToken = default);
}
