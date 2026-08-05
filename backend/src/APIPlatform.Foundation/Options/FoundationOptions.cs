namespace APIPlatform.Foundation.Options;

/// <summary>
/// Configuration options for the APIPlatform Foundation module.
/// </summary>
public class FoundationOptions
{
    /// <summary>
    /// The configuration section name for FoundationOptions.
    /// </summary>
    public const string SectionName = "APIPlatform:Foundation";

    /// <summary>
    /// Gets or sets the platform name.
    /// </summary>
    public string PlatformName { get; set; } = "EnterprisePlatform";

    /// <summary>
    /// Gets or sets the application environment name.
    /// </summary>
    public string Environment { get; set; } = "Development";
}
