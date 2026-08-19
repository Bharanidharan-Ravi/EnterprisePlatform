namespace APIPlatform.Database.Migration.Models;

/// <summary>
/// Outcome of one <see cref="Abstractions.IMigrationRunner.RunAsync"/> call. A run that applies
/// zero migrations because everything was already applied is still a successful result — that's
/// the idempotent case, not an error.
/// </summary>
public sealed record MigrationRunResult
{
    /// <summary>Migrations newly applied by this run, in the order they were applied.</summary>
    public required IReadOnlyList<AppliedMigration> Applied { get; init; }

    /// <summary>Migration ids that matched the active provider but were already recorded as
    /// applied — skipped without re-executing any DDL.</summary>
    public required IReadOnlyList<string> Skipped { get; init; }
}
