namespace APIPlatform.Foundation.Results;

/// <summary>
/// Common shape shared by all pass/fail platform result types (<see cref="OperationResult"/>,
/// <see cref="Result{T}"/>, <see cref="ValidationResult"/>). Not implemented by
/// <see cref="PagedResult{T}"/>, which describes a data shape rather than a pass/fail outcome.
/// </summary>
public interface IResult
{
    bool Succeeded { get; }
    IReadOnlyList<ErrorInfo> Errors { get; }
}

/// <summary>A single structured error, used across all platform result types.</summary>
public sealed record ErrorInfo
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    /// <summary>Field name this error relates to, if any (e.g. for validation failures).</summary>
    public string? Field { get; init; }
}
