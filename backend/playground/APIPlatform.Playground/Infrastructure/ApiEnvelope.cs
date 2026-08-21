namespace APIPlatform.Playground.Infrastructure;

/// <summary>
/// Response envelope matching the shape ui-platform-foundation's apiRequest()/unwrapResponse()
/// require: { success, data, error: { code, message, fieldErrors }, traceId }. Deliberately kept
/// application-level (not a change to APIPlatform.Shared, a platform assembly) — Playground's
/// existing controllers (Health/DatabaseValidation/etc.) are untouched and keep returning raw
/// bodies; only the endpoints this phase's frontend actually consumes (Auth, Employees) use it.
/// </summary>
public sealed class ApiEnvelope<T>
{
    public required bool Success { get; init; }
    public T? Data { get; init; }
    public ApiErrorDetail? Error { get; init; }
    public string? TraceId { get; init; }

    public static ApiEnvelope<T> Ok(T data, string? traceId = null) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiEnvelope<T> Fail(string code, string message, IDictionary<string, string[]>? fieldErrors = null, string? traceId = null) =>
        new() { Success = false, Error = new ApiErrorDetail { Code = code, Message = message, FieldErrors = fieldErrors }, TraceId = traceId };
}

public sealed class ApiErrorDetail
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public IDictionary<string, string[]>? FieldErrors { get; init; }
}

/// <summary>Non-generic convenience factory (avoids callers spelling out ApiEnvelope&lt;object&gt;).</summary>
public static class ApiEnvelope
{
    public static ApiEnvelope<T> Ok<T>(T data, string? traceId = null) => ApiEnvelope<T>.Ok(data, traceId);
    public static ApiEnvelope<object?> Fail(string code, string message, IDictionary<string, string[]>? fieldErrors = null, string? traceId = null) =>
        ApiEnvelope<object?>.Fail(code, message, fieldErrors, traceId);
}
