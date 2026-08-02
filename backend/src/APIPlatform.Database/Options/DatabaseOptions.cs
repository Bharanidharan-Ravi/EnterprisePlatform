using System.Data;

namespace APIPlatform.Data.Options;

/// <summary>
/// Connection and execution configuration for APIPlatform.Data. The package never reads
/// appsettings.json itself — the consuming application supplies everything here.
/// </summary>
public sealed class DatabaseOptions
{
    public required string ConnectionString { get; set; }
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 0;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.Zero;
    public string? DefaultSchema { get; set; }
    public bool EnableLogging { get; set; } = false;
    public IsolationLevel DefaultIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}
