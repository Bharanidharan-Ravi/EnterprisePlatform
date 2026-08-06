using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Playground.Models;
using APIPlatform.Validation.Abstractions;
using APIPlatform.Validation.Results;

namespace APIPlatform.Playground.Validators;

public class SampleRequestValidator : IValidator<SampleRequest>
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public Task<ValidationResult> ValidateAsync(SampleRequest instance, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            result.AddError(nameof(instance.Name), "Name cannot be empty.");
        }

        return Task.FromResult(result);
    }
}
