using System.Collections.Generic;

namespace APIPlatform.Shared.Responses;

/// <summary>
/// Represents a structured error response.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// A top-level error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// A list of detailed errors, if applicable.
    /// </summary>
    public List<ErrorDetail> Errors { get; set; } = [];
}

/// <summary>
/// Represents a specific detail of an error.
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// An error code for programmatic handling.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
