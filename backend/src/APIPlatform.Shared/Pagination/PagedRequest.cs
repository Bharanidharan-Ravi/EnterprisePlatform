namespace APIPlatform.Shared.Pagination;

/// <summary>
/// Represents a request for paginated data.
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// The page number to retrieve (1-based).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;
}
