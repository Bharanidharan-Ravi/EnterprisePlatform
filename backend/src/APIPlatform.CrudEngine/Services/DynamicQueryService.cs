using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql;
using APIPlatform.CrudEngine.Sql.Builders;
using APIPlatform.CrudEngine.Sql.Dialects;
using APIPlatform.Data.Execution;
using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Default <see cref="IDynamicQueryService"/> — turns a <see cref="DynamicQueryRequest"/> into a
/// parameterized SELECT and runs it through the same <see cref="IDatabaseExecutor"/>,
/// filter/sort/paging builders, and <see cref="ISqlDialectResolver"/> the metadata-driven CRUD
/// path already uses (Sql/Builders, Req 5/6/11), so this stays provider-agnostic for free.
///
/// <para>Unlike SqlQueryBuilder's EntityDefinition path — where table/field names come from
/// developer-authored config and are trusted as-is — TableName/Columns/Filters here arrive on the
/// request itself. They are therefore validated against a strict identifier allow-list before
/// being placed in generated SQL text; only filter *values* travel as SQL parameters, so this is
/// the one place in CrudEngine that also has to police identifiers, not just values.</para>
/// </summary>
public sealed class DynamicQueryService : IDynamicQueryService
{
    private const int MaxTop = 5000;

    private readonly IDatabaseExecutor _executor;
    private readonly IFilterClauseBuilder _filterBuilder;
    private readonly ISortClauseBuilder _sortBuilder;
    private readonly IPagingClauseBuilder _pagingBuilder;
    private readonly ISqlDialectResolver _dialectResolver;

    public DynamicQueryService(
        IDatabaseExecutor executor,
        IFilterClauseBuilder filterBuilder,
        ISortClauseBuilder sortBuilder,
        IPagingClauseBuilder pagingBuilder,
        ISqlDialectResolver dialectResolver)
    {
        _executor = executor;
        _filterBuilder = filterBuilder;
        _sortBuilder = sortBuilder;
        _pagingBuilder = pagingBuilder;
        _dialectResolver = dialectResolver;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        DynamicQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var sql = $"SELECT {string.Join(", ", request.Columns)} FROM {request.TableName}";

        var (whereFragment, filterParameters) = _filterBuilder.Build(request.Filters);
        if (!string.IsNullOrEmpty(whereFragment))
            sql += " WHERE " + whereFragment;

        // No caller-specified sort exists on this contract (it describes a filtered read, not a
        // list view) — SortClauseBuilder's default-key fallback still gives paging the ORDER BY
        // SQL Server's OFFSET/FETCH requires; which column is irrelevant since Top only bounds
        // row count here, it doesn't promise a particular ordering.
        sql += " " + _sortBuilder.Build(Array.Empty<SortSpec>(), new[] { request.Columns[0] });

        var dialect = _dialectResolver.Resolve();
        var top = Math.Clamp(request.Top, 1, MaxTop);
        sql = _pagingBuilder.Apply(sql, new PagingSpec(0, top), dialect);

        // T = object (what `dynamic` erases to as a generic argument) is Dapper's own convention
        // for "map each row to a DapperRow" — no per-shape POCO exists for an arbitrary column list.
        var rows = await _executor.QueryAsync<object>(sql, filterParameters, cancellationToken: cancellationToken);
        return rows.Select(ToDictionary).ToList();
    }

    private static void Validate(DynamicQueryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!SqlIdentifierValidator.IsValidQualifiedName(request.TableName))
            errors["TableName"] = new[] { $"'{request.TableName}' is not a valid table name." };

        if (request.Columns.Count == 0)
        {
            errors["Columns"] = new[] { "At least one column is required." };
        }
        else
        {
            var invalidColumns = request.Columns.Where(c => !SqlIdentifierValidator.IsValid(c)).ToList();
            if (invalidColumns.Count > 0)
                errors["Columns"] = invalidColumns.Select(c => $"'{c}' is not a valid column name.").ToArray();
        }

        var invalidFilterKeys = request.Filters.Keys.Where(k => !SqlIdentifierValidator.IsValid(k)).ToList();
        if (invalidFilterKeys.Count > 0)
            errors["Filters"] = invalidFilterKeys.Select(k => $"'{k}' is not a valid column name.").ToArray();

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static IReadOnlyDictionary<string, object?> ToDictionary(object row)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in (IDictionary<string, object>)row)
            result[key] = value;
        return result;
    }
}
