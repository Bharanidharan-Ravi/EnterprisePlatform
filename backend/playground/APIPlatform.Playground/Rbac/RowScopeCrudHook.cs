using System.Collections.Concurrent;
using System.Reflection;
using APIPlatform.CrudEngine.Hooks;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Contracts;

namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Applies row/data-level scoping to every CRUD read, for every entity, without a single line of
/// per-controller boilerplate — the platform's own designed extension point
/// (<see cref="ICrudPipelineHook"/>) rather than a call bolted into EmployeesController. Entities
/// with no RowPermissionRule are unaffected: <see cref="IRowAuthorizationFilterProvider"/> returns
/// RowFilterDescriptor.None for them and this hook does nothing.
///
/// <list type="bullet">
/// <item><b>List (OnBefore)</b> — pushes the descriptor's parameters into
/// CrudContext.AdditionalFilters. Hooks run before ExecutionPlanningStage, so the filter is part
/// of the generated WHERE clause and out-of-scope rows are never read out of the database at all,
/// let alone serialized. This is the point of doing it here rather than filtering the response.</item>
/// <item><b>GetByKey (OnAfter)</b> — the key lookup goes through IRepository, which takes no
/// filters, so scoping is enforced by discarding an out-of-scope row after it is loaded. The
/// caller then sees exactly what it would see for an id that does not exist: EmployeesController
/// maps null to <b>404, not 403</b>, deliberately — a 403 would confirm that the row exists and
/// leak the existence of data outside the caller's scope.</item>
/// </list>
///
/// <para><b>Writes.</b> Update and Delete are covered transitively: EmployeesController loads the
/// row through ICrudEngine.GetAsync first (for Update, to preserve CreatedOn; for Delete, to
/// return a clean 404), and that Get runs through this hook — so an out-of-scope id 404s before
/// any write is planned. A future controller calling UpdateAsync/DeleteAsync without loading first
/// would not be covered; enforcing scope on the write path itself needs the pre-image the pipeline
/// doesn't currently load, which is why it isn't done here.</para>
///
/// <para><b>Known fail-open branch (documented, not exploitable today).</b>
/// IRowAuthorizationFilterProvider returns RowFilterDescriptor.None both for "no rule applies" and
/// for "the caller was denied Row.Read" — the two are indistinguishable through that contract, and
/// this hook cannot tell them apart. A caller denied <c>employee.read</c> would therefore get an
/// unscoped query — but never reaches CrudEngine at all, because EmployeesController checks
/// <c>employee.read</c> via ICrudAuthorizationService and returns 403 first. Closing it properly
/// means surfacing AuthorizationResult.Allowed from the provider (an Rbac package change); logged
/// as a Phase 7 hardening item rather than worked around here.</para>
/// </summary>
public sealed class RowScopeCrudHook : CrudPipelineHookBase
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    private readonly IRowAuthorizationFilterProvider _rowFilters;

    public RowScopeCrudHook(IRowAuthorizationFilterProvider rowFilters) => _rowFilters = rowFilters;

    public override async Task OnBeforeAsync<TEntity>(CrudContext<TEntity> context)
    {
        if (context.Operation != CrudOperationType.List) return;

        var filter = await GetFilterAsync(context);
        if (filter.Parameters.Count == 0) return;

        foreach (var (column, value) in filter.Parameters)
            context.AdditionalFilters[column] = value;

        context.Diagnostics["RowScope.FilterName"] = filter.FilterName;
    }

    public override async Task OnAfterAsync<TEntity>(CrudContext<TEntity> context)
    {
        if (context.Operation != CrudOperationType.GetByKey) return;
        if (context.ExecutionResult is not TEntity entity) return;

        var filter = await GetFilterAsync(context);
        if (filter.Parameters.Count == 0) return;

        if (!Matches(entity, filter))
        {
            context.ExecutionResult = null;
            context.Diagnostics["RowScope.Excluded"] = true;
        }
    }

    private Task<RowFilterDescriptor> GetFilterAsync<TEntity>(CrudContext<TEntity> context) where TEntity : class =>
        // Lowercased to match the permission/rule key convention every seeded grant uses
        // ({entityKey}.{action}), while CrudContext.EntityName is the CLR type name ("Employee").
        _rowFilters.GetRowFilterAsync(context.EntityName.ToLowerInvariant(), context.CancellationToken);

    /// <summary>
    /// The in-memory counterpart of the WHERE clause the List path generates, kept deliberately
    /// equivalent to it: every parameter must match by equality, and a null required value matches
    /// nothing — mirroring SQL's three-valued <c>column = NULL</c>, so a user with no scope value
    /// gets the same empty result from GetByKey as from List. Strings compare case-insensitively
    /// because SQL Server's default collation does.
    /// </summary>
    private static bool Matches<TEntity>(TEntity entity, RowFilterDescriptor filter) where TEntity : class
    {
        foreach (var (column, required) in filter.Parameters)
        {
            if (required is null) return false;

            var property = PropertyCache.GetOrAdd((typeof(TEntity), column), key => key.Item1.GetProperty(
                key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            // A rule naming a column this entity doesn't have is a misconfiguration; fail closed.
            if (property is null) return false;

            var actual = property.GetValue(entity);
            var matches = actual is string actualText && required is string requiredText
                ? string.Equals(actualText, requiredText, StringComparison.OrdinalIgnoreCase)
                : Equals(actual, required);

            if (!matches) return false;
        }

        return true;
    }
}
