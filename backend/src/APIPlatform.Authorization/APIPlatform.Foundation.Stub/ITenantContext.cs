namespace APIPlatform.Foundation;

/// <summary>
/// STUB — placeholder for the real APIPlatform.Foundation package (frozen, not part of this
/// codebase yet). Master Plan Section 5.2: ITenantContext is defined in Foundation from day
/// one, resolved via DI by every tenant-scoped module — including Rbac (Section 5.1).
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
}
