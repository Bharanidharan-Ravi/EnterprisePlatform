using System;

namespace APIPlatform.Logging.Abstractions;

/// <summary>
/// A generic logging abstraction for the platform.
/// </summary>
/// <typeparam name="T">The type context for the logger.</typeparam>
public interface IPlatformLogger<T>
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void LogDebug(string message, params object?[] args);

    /// <summary>
    /// Logs an information message.
    /// </summary>
    void LogInformation(string message, params object?[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message, params object?[] args);

    /// <summary>
    /// Logs an error message with an optional exception.
    /// </summary>
    void LogError(Exception? exception, string message, params object?[] args);

    /// <summary>
    /// Logs a critical message with an optional exception.
    /// </summary>
    void LogCritical(Exception? exception, string message, params object?[] args);
}
