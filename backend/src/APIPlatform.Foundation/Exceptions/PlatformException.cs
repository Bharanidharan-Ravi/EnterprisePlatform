namespace APIPlatform.Foundation.Exceptions;

/// <summary>Cross-cutting classification of an error, for Logging/Diagnostics bucketing — never application-specific.</summary>
public enum ErrorCategory
{
    Unknown,
    Validation,
    NotFound,
    PermissionDenied,
    Infrastructure
}

/// <summary>
/// Common base for all Nucleus platform exceptions. Centralizes cross-cutting error metadata
/// so it isn't duplicated in every derived exception type.
/// </summary>
public abstract class PlatformException : Exception
{
    /// <summary>Machine-readable error code (e.g. for client-side localization/handling).</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Cross-cutting error classification, defaults to <see cref="ErrorCategory.Unknown"/>.</summary>
    public ErrorCategory Category { get; init; } = ErrorCategory.Unknown;

    /// <summary>Correlation id linking this exception to a request/trace, for Logging/Diagnostics.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Arbitrary additional structured detail about the failure.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    protected PlatformException(string message) : base(message) { }
    protected PlatformException(string message, Exception innerException) : base(message, innerException) { }
}
