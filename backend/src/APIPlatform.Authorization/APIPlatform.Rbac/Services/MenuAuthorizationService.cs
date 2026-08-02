using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class MenuAuthorizationService : IMenuAuthorizationService
{
    private readonly IPermissionEvaluator _evaluator;

    public MenuAuthorizationService(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public async Task<IReadOnlyList<MenuItem>> FilterMenuAsync(IReadOnlyList<MenuItem> menu, CancellationToken cancellationToken = default)
    {
        var result = new List<MenuItem>();

        foreach (var item in menu)
        {
            if (item.RequiredPermissionKey is not null)
            {
                var request = new AuthorizationRequest
                {
                    ResourceType = ResourceType.Menu,
                    ResourceKey = item.Key,
                    Action = "View",
                    PermissionKeyOverride = item.RequiredPermissionKey
                };

                var evaluated = await _evaluator.EvaluateAsync(request, cancellationToken);
                if (!evaluated.Allowed)
                    continue;
            }

            var children = item.Children.Count > 0
                ? await FilterMenuAsync(item.Children, cancellationToken)
                : Array.Empty<MenuItem>();

            result.Add(item with { Children = children });
        }

        return result;
    }
}
