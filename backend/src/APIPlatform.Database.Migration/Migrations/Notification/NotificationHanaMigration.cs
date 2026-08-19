using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;

namespace APIPlatform.Database.Migration.Migrations.Notification;

/// <summary>Creates APIPlatform.Notification's three tables (Notifications, NotificationTargets,
/// NotificationUserStates) plus their indexes/constraints on SAP HANA. Shares
/// <see cref="MigrationId"/>/<see cref="Version"/> with <c>NotificationSqlServerMigration</c> —
/// one logical migration, tracked once in history regardless of which provider applied it. The
/// transaction the runner passes to <see cref="ApplyAsync"/> is always null here (HANA DDL
/// auto-commits) — each CREATE statement is its own atomic unit; a failure partway leaves earlier
/// statements in this run already committed, which is why this migration is a fixed, additive,
/// one-time set of CREATEs rather than something meant to be retried after partial failure.</summary>
public sealed class NotificationHanaMigration : IMigration
{
    public string MigrationId => "Notification.Schema.v1";

    public int Version => 1;

    public string Description => "Creates the Notifications, NotificationTargets, and NotificationUserStates tables.";

    public DatabaseProvider SupportedProvider => DatabaseProvider.Hana;

    public async Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
    {
        foreach (var statement in NotificationSchemaSql.HanaStatements)
            await executor.ExecuteAsync(statement, transaction: transaction, cancellationToken: cancellationToken);
    }
}
