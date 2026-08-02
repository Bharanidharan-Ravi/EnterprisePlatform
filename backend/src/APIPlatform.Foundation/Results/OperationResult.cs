namespace APIPlatform.Foundation.Results;

/// <summary>Non-generic outcome of an operation that returns no value (e.g. Delete).</summary>
public sealed record OperationResult : IResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ErrorInfo> Errors { get; init; } = Array.Empty<ErrorInfo>();

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult Failure(params ErrorInfo[] errors) => new() { Succeeded = false, Errors = errors };
}
