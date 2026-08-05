namespace APIPlatform.Configuration.Options;

/// <summary>
/// Simple platform options class for testing configuration bindings.
/// </summary>
public class PlatformOptions
{
    /// <summary>
    /// The application name.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// The application version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
