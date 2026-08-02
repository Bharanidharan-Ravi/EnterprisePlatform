using APIPlatform.Rbac.Common;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;
using Nucleus.SharedSchema;

namespace APIPlatform.Rbac.Pipeline.Stages;

/// <summary>
/// STAGE 1 (module-specific name: "Permission Resolution Stage").
/// Understands the request and resolves which PermissionKey(s) are required. Never executes
/// business logic or makes an allow/deny decision — that is Execution Stage's job.
/// </summary>
public sealed class PermissionResolutionStage : IAuthorizationStage
{
    private readonly ISharedSchemaProvider? _schemaProvider;

    // ISharedSchemaProvider is optional: Rbac must remain usable even if Shared Schema isn't
    // wired up yet in a given deployment (matches Foundation/SharedSchema being separate,
    // independently-consumable packages per Hard Rule 3).
    public PermissionResolutionStage(ISharedSchemaProvider? schemaProvider = null)
    {
        _schemaProvider = schemaProvider;
    }

    public async Task ExecuteAsync(AuthorizationPipelineState state, CancellationToken cancellationToken)
    {
        var request = state.Request;

        var keys = new List<string>
        {
            request.PermissionKeyOverride ?? PermissionKeyBuilder.Build(request.ResourceType, request.ResourceKey, request.Action)
        };

        if (request.ResourceType == ResourceType.Field && _schemaProvider is not null && request.FieldKey is not null)
        {
            var entity = await _schemaProvider.GetEntityMetadataAsync(request.ResourceKey, cancellationToken);
            var field = entity?.Fields.FirstOrDefault(f => f.FieldKey == request.FieldKey);
            if (field?.DefaultPermissionKey is { } defaultKey)
            {
                keys.Add(defaultKey);
            }
        }

        state.RequiredPermissionKeys = keys;
    }
}
