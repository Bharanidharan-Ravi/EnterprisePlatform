namespace Nucleus.SharedSchema;

/// <summary>
/// STUB — placeholder for the real Nucleus.SharedSchema package (frozen, not part of this
/// codebase yet). Master Plan Section 6.1 lists "Permission requirements (which roles can
/// read/write this field)" as part of the one shared schema format. Rbac reads this as the
/// DEFAULT field permission key; explicit grants in Rbac can still override it per tenant.
/// </summary>
public sealed record FieldMetadata(string FieldKey, string? DefaultPermissionKey);
