using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;
using APIPlatform.Rbac.Models;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// The app-supplied half of row-level scoping. RowPermissionRule stores only a delegate NAME
/// (FilterDelegateKey); the predicate behind that name is registered here, because — per
/// RowPermissionRule's own doc comment — "Rbac never contains a domain predicate itself (e.g. no
/// 'DepartmentId == currentUser.DepartmentId' literal in this package; that belongs to the
/// generated app that registers the delegate)".
///
/// <para><b>Descriptor convention this host uses:</b> RowFilterDescriptor.Parameters is a map of
/// {column name → required value}, ANDed as equality. <see cref="RowScopeCrudHook"/> is the only
/// reader of that convention; it turns the map into CrudContext.AdditionalFilters for a List and
/// into an in-memory match check for a GetByKey. Rbac itself stays agnostic — the descriptor is a
/// "provider-agnostic description of a row-level filter" and it is the app's job to interpret it.</para>
/// </summary>
public static class EmployeeRowFilters
{
    /// <summary>The name stored in RbacRowPermissionRules.FilterDelegateKey for Employee.</summary>
    public const string OwnDepartment = "OwnDepartment";

    /// <summary>
    /// The escape hatch that lets a role opt OUT of scoping. A rule is attached per (tenant,
    /// entity) — IRowPermissionRuleStore.GetRulesAsync takes no role — so "admins see everything"
    /// cannot be expressed by attaching the rule to some roles and not others; it has to be a
    /// decision the delegate itself makes, and a permission key is the platform's existing way to
    /// say "this role may". Follows the established {entityKey}.{action} shape.
    /// </summary>
    public const string UnscopedReadPermissionKey = "employee.read.all";

    /// <summary>Employee column the department scope compares against.</summary>
    public const string DepartmentColumn = "Department";

    /// <summary>
    /// "Show only rows in the caller's own department", unless the caller holds
    /// <see cref="UnscopedReadPermissionKey"/>.
    ///
    /// <para>Fail-closed on a missing scope value: a user with no department_id yields
    /// <c>Department = NULL</c>, which SQL's three-valued logic matches against nothing, so they
    /// see zero rows rather than all of them. That is the intended posture — a user who is scoped
    /// but has no scope value assigned is a misconfiguration, and the safe reading of a
    /// misconfiguration is "nothing", not "everything".</para>
    /// </summary>
    public static Task<RowFilterDescriptor> OwnDepartmentAsync(AuthorizationContext context)
    {
        if (context.EffectivePermissions?.AllowedKeys.Contains(UnscopedReadPermissionKey) == true)
            return Task.FromResult(RowFilterDescriptor.None);

        context.Claims.TryGetValue(ScopeKeys.Department, out var department);

        return Task.FromResult(new RowFilterDescriptor
        {
            FilterName = OwnDepartment,
            Parameters = new Dictionary<string, object?> { [DepartmentColumn] = department }
        });
    }

    /// <summary>Registers every delegate this host defines. Called once at startup, before the
    /// first request — see EmployeeModuleInitializationService.</summary>
    public static void RegisterAll(IRowFilterRegistry registry) =>
        registry.Register(OwnDepartment, OwnDepartmentAsync);
}
