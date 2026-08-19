namespace APIPlatform.Database.Migration.Models;

/// <summary>One row of the migration history — a migration that has been applied. Id and
/// AppliedOnUtc are runner-generated (never database defaults), consistent with every other
/// platform table's API-generated ids/timestamps.</summary>
public sealed record AppliedMigration
{
    public required string Id { get; init; }

    public required string MigrationId { get; init; }

    public required int Version { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset AppliedOnUtc { get; init; }
}
