using System.Collections.Generic;

namespace APIPlatform.Shared.Pagination;

/// <summary>
/// Represents a paginated response containing a collection of items.
/// </summary>
/// <typeparam name="T">The type of items in the pagination response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// The total number of items available across all pages.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// The current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)((TotalCount + PageSize - 1) / PageSize) : 0;
}
