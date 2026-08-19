using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Data.Transactions;
using APIPlatform.Database.Migration.Abstractions;

namespace APIPlatform.Database.Migration.Migrations.Notification;

/// <summary>Creates APIPlatform.Notification's three tables (Notifications, NotificationTargets,
/// NotificationUserStates) plus their indexes/constraints on SQL Server. Shares
/// <see cref="MigrationId"/>/<see cref="Version"/> with <c>NotificationHanaMigration</c> — one
/// logical migration, tracked once in history regardless of which provider applied it.</summary>
public sealed class NotificationSqlServerMigration : IMigration
{
    public string MigrationId => "Notification.Schema.v1";

    public int Version => 1;

    public string Description => "Creates the Notifications, NotificationTargets, and NotificationUserStates tables.";

    public DatabaseProvider SupportedProvider => DatabaseProvider.SqlServer;

    public async Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default)
    {
        foreach (var statement in NotificationSchemaSql.SqlServerStatements)
            await executor.ExecuteAsync(statement, transaction: transaction, cancellationToken: cancellationToken);
    }
}
