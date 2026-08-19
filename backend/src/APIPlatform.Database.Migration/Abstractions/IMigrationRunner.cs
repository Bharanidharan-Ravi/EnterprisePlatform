using APIPlatform.Database.Migration.Models;

namespace APIPlatform.Database.Migration.Abstractions;

/// <summary>
/// Applies every registered <see cref="IMigration"/> matching the application's configured
/// <c>DatabaseOptions.Provider</c> that has not already been applied, in ascending
/// <see cref="IMigration.Version"/> order. This is always an explicit, caller-invoked step — no
/// implementation registers itself to run automatically on host startup.
/// </summary>
public interface IMigrationRunner
{
    Task<MigrationRunResult> RunAsync(CancellationToken cancellationToken = default);
}
