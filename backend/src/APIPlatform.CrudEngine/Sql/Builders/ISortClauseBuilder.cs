using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Sql.Builders;

/// <summary>Builds an ORDER BY clause from OperationPlan.Sorting.</summary>
public interface ISortClauseBuilder
{
    string Build(IReadOnlyList<SortSpec> sorting, IReadOnlyList<string> defaultKeyFieldNames);
}

public sealed class SortClauseBuilder : ISortClauseBuilder
{
    public string Build(IReadOnlyList<SortSpec> sorting, IReadOnlyList<string> defaultKeyFieldNames)
    {
        var specs = sorting.Count > 0
            ? sorting
            : defaultKeyFieldNames.Select(k => new SortSpec(k)).ToList();

        return "ORDER BY " + string.Join(", ", specs.Select(s => $"{s.FieldName}{(s.Descending ? " DESC" : "")}"));
    }
}
