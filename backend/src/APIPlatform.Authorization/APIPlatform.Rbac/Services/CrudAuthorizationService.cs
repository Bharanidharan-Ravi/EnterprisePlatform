using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class CrudAuthorizationService : ICrudAuthorizationService
{
    private readonly IPermissionEvaluator _evaluator;

    public CrudAuthorizationService(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public Task<AuthorizationResult> AuthorizeAsync(string entityKey, string action, CancellationToken cancellationToken = default) =>
        _evaluator.EvaluateAsync(new AuthorizationRequest
        {
            ResourceType = ResourceType.Crud,
            ResourceKey = entityKey,
            Action = action
        }, cancellationToken);
}
