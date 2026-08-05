namespace APIPlatform.Logging.Options;

/// <summary>
/// Configuration options for the APIPlatform Logging module.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "APIPlatform:Logging";

    /// <summary>
    /// Indicates whether console logging is enabled.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Indicates whether standard output should include sensitive data (default is false).
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;
}
