using System.Collections.Generic;
using System.Linq;

namespace APIPlatform.Validation.Results;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; } = new();

    /// <summary>
    /// Adds an error to the result.
    /// </summary>
    public void AddError(string propertyName, string errorMessage)
    {
        Errors.Add(new ValidationError { PropertyName = propertyName, ErrorMessage = errorMessage });
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The validation error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
