using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Validation.Results;

namespace APIPlatform.Validation.Abstractions;

/// <summary>
/// Defines a service that acts as the entry point for executing validations.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates an object using all registered validators for its type.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A combined validation result.</returns>
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}
