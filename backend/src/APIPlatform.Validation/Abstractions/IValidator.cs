using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Validation.Results;

namespace APIPlatform.Validation.Abstractions;

/// <summary>
/// Defines a validator for a specific type.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified instance asynchronously.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A validation result.</returns>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
