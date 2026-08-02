namespace APIPlatform.Foundation.Results;

/// <summary>A single page of results plus paging metadata. Consumed by Search, Grid, and Reporting.</summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
