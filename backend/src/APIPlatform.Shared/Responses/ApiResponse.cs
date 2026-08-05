namespace APIPlatform.Shared.Responses;

/// <summary>
/// Represents a generic API response wrapper.
/// </summary>
/// <typeparam name="T">The type of the data returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// The actual data of the response.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// An optional message providing more context about the response.
    /// </summary>
    public string? Message { get; set; }
}
