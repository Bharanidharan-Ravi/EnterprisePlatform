using System.Collections.Concurrent;
using System.Reflection;
using APIPlatform.CrudEngine.Hooks;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Applies field-level masking to every CRUD read, for every entity, through the platform's own
/// extension point — same shape and same reasoning as <see cref="RowScopeCrudHook"/>: generic over
/// <c>TEntity</c>, keyed off <c>context.EntityName</c>, so any entity with a seeded
/// <see cref="FieldPermissionRule"/> gets masking for free, with zero per-controller code.
/// Entities with no rule are untouched (<see cref="IFieldAuthorizationService.GetFieldMaskAsync"/>
/// returns an empty mask for them).
///
/// <c>OnAfterAsync</c> only (GetByKey and List) — the masked value must never reach the JSON
/// payload, so it is nulled out of <c>context.ExecutionResult</c> before ResponseMappingStage builds
/// the response, not filtered client-side.
///
/// <b>Write-side enforcement is deliberately not implemented here.</b> The natural design —
/// reject a write to a field the caller only holds <see cref="FieldAccess.Read"/> (or
/// <see cref="FieldAccess.None"/>) on — needs a hook to be able to short-circuit the pipeline from
/// <c>OnBeforeAsync</c>, and <c>CrudPipeline&lt;TEntity&gt;.RunAsync</c> does not currently re-check
/// <c>CrudContext.ShortCircuited</c> after the OnBeforeAsync hook loop (only after ValidationStage),
/// so setting it there is silently a no-op today — the same class of structural gap Phase 2 found in
/// <c>RequestedFilters</c>. Not exploitable in this host: every role that can currently reach
/// Create/Update (only <c>employee-admin</c>) also holds Write on every masked field, by design of
/// the seeded grants below. Revisit alongside the pipeline fix if a write-capable-but-field-restricted
/// role is ever added (e.g. Phase 3's "Department Head" row in the role ladder table).
/// </summary>
public sealed class FieldMaskCrudHook : CrudPipelineHookBase
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    private readonly IFieldAuthorizationService _fieldAuth;

    public FieldMaskCrudHook(IFieldAuthorizationService fieldAuth) => _fieldAuth = fieldAuth;

    public override async Task OnAfterAsync<TEntity>(CrudContext<TEntity> context)
    {
        if (context.Operation is not (CrudOperationType.GetByKey or CrudOperationType.List)) return;

        var mask = await _fieldAuth.GetFieldMaskAsync(context.EntityName.ToLowerInvariant(), context.CancellationToken);
        var hiddenFields = mask.FieldAccess.Where(kv => kv.Value == FieldAccess.None).Select(kv => kv.Key).ToList();
        if (hiddenFields.Count == 0) return;

        switch (context.ExecutionResult)
        {
            case TEntity entity:
                Mask(entity, hiddenFields);
                break;
            case IReadOnlyList<TEntity> entities:
                foreach (var entity in entities) Mask(entity, hiddenFields);
                break;
        }

        context.Diagnostics["FieldMask.HiddenFields"] = hiddenFields;
    }

    private static void Mask<TEntity>(TEntity entity, IReadOnlyList<string> hiddenFields) where TEntity : class
    {
        foreach (var fieldKey in hiddenFields)
        {
            var property = PropertyCache.GetOrAdd((typeof(TEntity), fieldKey), key => key.Item1.GetProperty(
                key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            // A rule naming a field this entity doesn't have, or a non-nullable value type
            // (int/bool/DateTimeOffset) that can't hold null — skip rather than throw. Masking a
            // field is a narrowing operation; a misconfigured rule should never turn into a 500.
            if (property is null || !property.CanWrite) continue;
            if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null) continue;

            property.SetValue(entity, null);
        }
    }
}
