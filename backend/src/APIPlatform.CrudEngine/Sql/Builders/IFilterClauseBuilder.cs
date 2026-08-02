namespace APIPlatform.CrudEngine.Sql.Builders;

/// <summary>Builds a WHERE clause fragment from OperationPlan.Filters. Split out per Req 6.
/// Current implementation supports equality filters (Field = @Field); extend here for
/// range/contains operators without touching the query builder that composes it.</summary>
public interface IFilterClauseBuilder
{
    (string WhereFragment, IReadOnlyDictionary<string, object?> Parameters) Build(IReadOnlyDictionary<string, object?> filters);
}

public sealed class FilterClauseBuilder : IFilterClauseBuilder
{
    public (string WhereFragment, IReadOnlyDictionary<string, object?> Parameters) Build(IReadOnlyDictionary<string, object?> filters)
    {
        if (filters.Count == 0) return (string.Empty, filters);

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var clauses = new List<string>();
        foreach (var (field, value) in filters)
        {
            var paramName = $"Filter_{field}";
            clauses.Add($"{field} = @{paramName}");
            parameters[paramName] = value;
        }
        return (string.Join(" AND ", clauses), parameters);
    }
}
