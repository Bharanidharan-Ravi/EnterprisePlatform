namespace APIPlatform.CrudEngine.Models;

/// <summary>Immutable description of a query, produced by IOperationPlanner and consumed by
/// Sql/Builders — SQL is never generated directly from a raw request (Req 5). Currently scoped
/// to List/query operations; single-row mutations still flow through the existing SqlQueryBuilder
/// path inside CompositeRepository, which is already metadata-driven and provider-agnostic.</summary>
public sealed class OperationPlan
{
    public required CrudOperationType Operation { get; init; }
    public IReadOnlyDictionary<string, object?> Filters { get; init; } = new Dictionary<string, object?>();
    public IReadOnlyList<SortSpec> Sorting { get; init; } = Array.Empty<SortSpec>();
    public PagingSpec? Paging { get; init; }
}

public sealed record SortSpec(string FieldName, bool Descending = false);

public sealed record PagingSpec(int Skip, int Take);
