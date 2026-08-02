using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql.Builders;
using APIPlatform.CrudEngine.Sql.Dialects;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Sql;

/// <summary>
/// Composes filter/sort/paging clause builders on top of the existing base SELECT
/// (SqlQueryBuilder.SelectAll) to satisfy an OperationPlan (Req 5, Req 6). Insert/Update/Delete
/// remain in SqlQueryBuilder unchanged — those don't vary by provider or need composable clauses.
/// </summary>
public sealed class QuerySqlBuilder
{
    private readonly IFilterClauseBuilder _filters;
    private readonly ISortClauseBuilder _sort;
    private readonly IPagingClauseBuilder _paging;

    public QuerySqlBuilder(IFilterClauseBuilder filters, ISortClauseBuilder sort, IPagingClauseBuilder paging)
    {
        _filters = filters;
        _sort = sort;
        _paging = paging;
    }

    public (string Sql, IReadOnlyDictionary<string, object?> Parameters) Build(
        EntityDefinition def,
        OperationPlan plan,
        IReadOnlyList<string> keyFieldNames,
        ISqlDialect dialect,
        object? tenantId)
    {
        var baseSelect = SqlQueryBuilder.SelectAll(def).TrimEnd();
        var (whereFragment, filterParams) = _filters.Build(plan.Filters);

        var hasTenantWhere = baseSelect.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        var sql = baseSelect;
        if (!string.IsNullOrEmpty(whereFragment))
            sql += (hasTenantWhere ? " AND " : " WHERE ") + whereFragment;

        sql += " " + _sort.Build(plan.Sorting, keyFieldNames);
        sql = _paging.Apply(sql, plan.Paging, dialect);

        var parameters = new Dictionary<string, object?>(filterParams, StringComparer.OrdinalIgnoreCase);
        if (def.IsTenantScoped) parameters["TenantId"] = tenantId;

        return (sql, parameters);
    }
}
