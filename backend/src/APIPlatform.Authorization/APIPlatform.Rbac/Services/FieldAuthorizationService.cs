using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Services;

public sealed class FieldAuthorizationService : IFieldAuthorizationService
{
    private readonly IPermissionEvaluator _evaluator;

    public FieldAuthorizationService(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public async Task<FieldMaskDescriptor> GetFieldMaskAsync(string entityKey, CancellationToken cancellationToken = default)
    {
        var request = new AuthorizationRequest
        {
            ResourceType = ResourceType.Field,
            ResourceKey = entityKey,
            Action = "Read",
            FieldKey = "*"
        };

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);
        return result.FieldMask ?? FieldMaskDescriptor.Empty;
    }
}
