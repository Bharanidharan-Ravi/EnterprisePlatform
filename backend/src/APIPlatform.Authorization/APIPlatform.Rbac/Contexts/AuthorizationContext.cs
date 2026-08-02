namespace APIPlatform.Rbac.Contexts;

/// <summary>
/// Populated during the Context Enrichment Stage. Carried through the remaining stages and
/// into every IAuthorizationHook callback and every registered Policy/RowFilter delegate.
///
/// Extensibility map — every concern raised in review has a home already, so no structural
/// change should be required as the platform grows:
///   User             -> UserId
///   Tenant           -> TenantId
///   Resource/Action  -> Request (ResourceType, ResourceKey, Action, FieldKey)
///   Claims           -> Claims
///   Request Metadata -> Metadata
/// All properties beyond the two `required` identity fields are additive with safe defaults,
/// so future fields can be appended without breaking existing callers of
/// IAuthorizationContextFactory.CreateAsync or any AuthorizationContext object-initializer.
/// </summary>
public sealed class AuthorizationContext
{
    public required string UserId { get; init; }
    public required string TenantId { get; init; }
    public required AuthorizationRequest Request { get; init; }
    public PermissionSet? EffectivePermissions { get; set; }

    /// <summary>
    /// Identity claims relevant to authorization (e.g. department, region) as resolved by
    /// Auth/Foundation. Deliberately a simple string map, not a hard dependency on
    /// System.Security.Claims.Claim, to keep Rbac framework-agnostic. Populated by
    /// IAuthorizationContextFactory; empty by default so existing factories remain valid.
    /// </summary>
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();

    /// <summary>Free-form bag for hook/extension-point data. Never read by core pipeline logic.</summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
}
