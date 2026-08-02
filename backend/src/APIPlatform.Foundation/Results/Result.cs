namespace APIPlatform.Foundation.Results;

/// <summary>
/// Standard outcome envelope for an operation that returns a value. Intended to be the common
/// return shape across Auth, Data, Workflow, Notification, Storage, Search, and Sync so API
/// consumers deal with one consistent contract.
/// </summary>
public sealed record Result<T> : IResult
{
    public required bool Succeeded { get; init; }
    public T? Value { get; init; }
    public IReadOnlyList<ErrorInfo> Errors { get; init; } = Array.Empty<ErrorInfo>();

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static Result<T> Failure(params ErrorInfo[] errors) => new() { Succeeded = false, Errors = errors };

    /// <summary>Convenience conversion so callers can return a bare value where a Result&lt;T&gt; is expected.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
