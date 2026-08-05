using System;
using APIPlatform.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace APIPlatform.Logging.Services;

/// <summary>
/// The default implementation of <see cref="IPlatformLogger{T}"/> that wraps <see cref="ILogger{TCategoryName}"/>.
/// </summary>
/// <typeparam name="T">The type context for the logger.</typeparam>
public class PlatformLogger<T> : IPlatformLogger<T>
{
    private readonly ILogger<T> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformLogger{T}"/> class.
    /// </summary>
    /// <param name="logger">The underlying Microsoft logger instance.</param>
    public PlatformLogger(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void LogDebug(string message, params object?[] args)
    {
        _logger.LogDebug(message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(string message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(string message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <inheritdoc/>
    public void LogError(Exception? exception, string message, params object?[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(Exception? exception, string message, params object?[] args)
    {
        _logger.LogCritical(exception, message, args);
    }
}
