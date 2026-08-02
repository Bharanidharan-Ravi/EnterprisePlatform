using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class RowAuthorizationFilterProvider : IRowAuthorizationFilterProvider
{
    private readonly IPermissionEvaluator _evaluator;

    public RowAuthorizationFilterProvider(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public async Task<RowFilterDescriptor> GetRowFilterAsync(string entityKey, CancellationToken cancellationToken = default)
    {
        var request = new AuthorizationRequest
        {
            ResourceType = ResourceType.Row,
            ResourceKey = entityKey,
            Action = "Read"
        };

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);
        return result.RowFilter ?? RowFilterDescriptor.None;
    }
}
