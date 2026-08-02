using APIPlatform.Rbac.Contexts;

namespace APIPlatform.Rbac.Contracts;

/// <summary>Whole-entity CRUD authorization (Create/Read/Update/Delete), consumed by CrudEngine.</summary>
public interface ICrudAuthorizationService
{
    Task<AuthorizationResult> AuthorizeAsync(string entityKey, string action, CancellationToken cancellationToken = default);
}
