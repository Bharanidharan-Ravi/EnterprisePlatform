namespace APIPlatform.Rbac.Models;

/// <summary>
/// Row-level rule. FilterDelegateKey looks up a named filter-builder function in
/// IRowFilterRegistry, registered by the consuming app — Rbac never contains a domain
/// predicate itself (e.g. no "DepartmentId == currentUser.DepartmentId" literal in this
/// package; that belongs to the generated app that registers the delegate).
/// </summary>
public sealed class RowPermissionRule
{
    public required string EntityKey { get; init; }
    public required string FilterDelegateKey { get; init; }
    public bool TenantScoped { get; init; } = true;
}
