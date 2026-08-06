using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Validation.Abstractions;
using APIPlatform.Validation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Validation.Services;

/// <summary>
/// The default implementation of <see cref="IValidationService"/> that executes all registered validators.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve validators.</param>
    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var finalResult = new ValidationResult();
        if (instance == null)
        {
            finalResult.AddError(string.Empty, "Instance cannot be null.");
            return finalResult;
        }

        var validators = _serviceProvider.GetServices<IValidator<T>>();
        
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            if (!result.IsValid)
            {
                finalResult.Errors.AddRange(result.Errors);
            }
        }

        return finalResult;
    }
}
