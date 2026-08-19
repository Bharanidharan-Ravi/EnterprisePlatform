using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.Database.Migration.Models;

/// <summary>
/// Thrown when a migration fails to apply. Carries which migration failed and which ones already
/// succeeded earlier in the same run, so a caller (future CLI/HTTP layer) can report exactly how
/// far the run got instead of just "it failed" — the runner stops at the first failure rather
/// than continuing past a schema it doesn't know is consistent.
///
/// Derives from <see cref="PlatformException"/> directly rather than
/// <c>APIPlatform.Data.Exceptions.DatabaseException</c> — that type is sealed (by design, so
/// APIPlatform.Database consumers depend on one concrete exception type across providers rather
/// than subclassing it), so this package's own failure type follows the same
/// PlatformException-derived shape (Category, ErrorCode, etc.) alongside it instead.
/// </summary>
public sealed class MigrationException : PlatformException
{
    public required string FailedMigrationId { get; init; }

    public required IReadOnlyList<AppliedMigration> AppliedBeforeFailure { get; init; }

    public MigrationException(string message, Exception innerException)
        : base(message, innerException) => Category = ErrorCategory.Infrastructure;
}
