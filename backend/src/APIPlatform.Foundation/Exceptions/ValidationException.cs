namespace APIPlatform.Foundation.Exceptions;

/// <summary>Thrown when one or more field or cross-field validation rules fail.</summary>
public sealed class ValidationException : PlatformException
{
    /// <summary>Validation errors keyed by field name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
        Category = ErrorCategory.Validation;
    }
}
