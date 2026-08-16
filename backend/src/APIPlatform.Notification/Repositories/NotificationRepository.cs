using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Execution;
using APIPlatform.Notification.Abstractions;
using APIPlatform.Notification.Models;
using APIPlatform.Notification.Sql;
using APIPlatform.Notification.Sql.Dialects;

namespace APIPlatform.Notification.Repositories;

/// <summary>Dapper/IDatabaseExecutor-backed <see cref="INotificationRepository"/>. All SQL text comes from <see cref="NotificationSqlBuilder"/>; this class only binds parameters and maps rows.</summary>
public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDatabaseExecutor _executor;
    private readonly INotificationSqlDialectResolver _dialectResolver;

    public NotificationRepository(IDatabaseExecutor executor, INotificationSqlDialectResolver dialectResolver)
    {
        _executor = executor;
        _dialectResolver = dialectResolver;
    }

    public async Task<NotificationRecord> InsertAsync(
        NotificationRecord notification, IReadOnlyList<NotificationTargetRule> targets, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();

        await using var transaction = await _executor.BeginTransactionAsync(cancellationToken: cancellationToken);

        var insertNotificationSql = NotificationSqlBuilder.InsertNotification(dialect);
        await _executor.ExecuteAsync(insertNotificationSql, new Dictionary<string, object?>
        {
            ["Id"] = notification.Id,
            ["Application"] = notification.Application,
            ["EntityType"] = notification.EntityType,
            ["EntityId"] = notification.EntityId,
            ["EventType"] = notification.EventType,
            ["Title"] = notification.Title,
            ["Message"] = notification.Message,
            ["Data"] = notification.Data,
            ["CreatedBy"] = notification.CreatedBy,
            ["CreatedOnUtc"] = notification.CreatedOnUtc.UtcDateTime
        }, transaction: transaction, cancellationToken: cancellationToken);

        var insertTargetSql = NotificationSqlBuilder.InsertTarget(dialect);
        foreach (var rule in targets)
        {
            await _executor.ExecuteAsync(insertTargetSql, new Dictionary<string, object?>
            {
                ["Id"] = Guid.NewGuid().ToString(),
                ["NotificationId"] = notification.Id,
                ["TargetKind"] = (byte)rule.Kind,
                ["TargetValue"] = rule.Value,
                ["IsExclusion"] = rule.IsExclusion
            }, transaction: transaction, cancellationToken: cancellationToken);
        }

        // No commit before this point means dispose rolls the whole insert back automatically —
        // a notification is never left partially persisted (e.g. row with no targets).
        await transaction.CommitAsync(cancellationToken);

        return notification;
    }

    public async Task<IReadOnlyList<NotificationRecord>> ListForRecipientAsync(
        string application, NotificationRecipient recipient, DateTimeOffset? since, int skip, int take, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = NotificationSqlBuilder.RecipientMatch(dialect, recipient.GroupCodes.Count, since.HasValue, countOnly: false, skip: skip, take: take);
        var parameters = BuildRecipientParameters(application, recipient, since);

        var rows = await _executor.QueryAsync<NotificationRow>(sql, parameters, cancellationToken: cancellationToken);
        return rows.Select(ToRecord).ToList();
    }

    public async Task<int> CountForRecipientAsync(
        string application, NotificationRecipient recipient, DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = NotificationSqlBuilder.RecipientMatch(dialect, recipient.GroupCodes.Count, since.HasValue, countOnly: true);
        var parameters = BuildRecipientParameters(application, recipient, since);

        return await _executor.ExecuteScalarAsync<int>(sql, parameters, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRecord>> ListForEntityAsync(
        string application, string entityType, string entityId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = NotificationSqlBuilder.EntityHistory(dialect, skip, take);
        var parameters = new Dictionary<string, object?>
        {
            ["Application"] = application,
            ["EntityType"] = entityType,
            ["EntityId"] = entityId
        };

        var rows = await _executor.QueryAsync<NotificationRow>(sql, parameters, cancellationToken: cancellationToken);
        return rows.Select(ToRecord).ToList();
    }

    public async Task<NotificationUserState?> GetUserStateAsync(string application, string userId, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        var sql = NotificationSqlBuilder.GetUserState(dialect);
        var parameters = new Dictionary<string, object?> { ["UserId"] = userId, ["Application"] = application };

        var row = await _executor.QuerySingleOrDefaultAsync<NotificationUserStateRow>(sql, parameters, cancellationToken: cancellationToken);
        return row is null ? null : ToState(row);
    }

    public Task SetLastReadOnAsync(string application, string userId, DateTimeOffset readOnUtc, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        return UpsertCursorAsync(
            NotificationSqlBuilder.UpdateLastReadOn(dialect),
            NotificationSqlBuilder.InsertUserStateWithLastReadOn(dialect),
            application, userId, readOnUtc, cancellationToken);
    }

    public Task SetLastSyncedOnAsync(string application, string userId, DateTimeOffset syncedOnUtc, CancellationToken cancellationToken = default)
    {
        var dialect = _dialectResolver.Resolve();
        return UpsertCursorAsync(
            NotificationSqlBuilder.UpdateLastSyncedOn(dialect),
            NotificationSqlBuilder.InsertUserStateWithLastSyncedOn(dialect),
            application, userId, syncedOnUtc, cancellationToken);
    }

    /// <summary>
    /// Update-first, insert-on-fallback upsert for a single-user-state column (no MERGE, for
    /// SQL Server/HANA portability). On the rare race where two first-touches for the same
    /// (user, application) both miss the UPDATE and both attempt the INSERT, the loser's insert
    /// fails; rather than surface that as an error it retries the UPDATE once, since the row
    /// now exists — a genuine infrastructure failure still surfaces if that retry also affects
    /// zero rows, so this never silently swallows a real failure.
    /// </summary>
    private async Task UpsertCursorAsync(
        string updateSql, string insertSql, string application, string userId, DateTimeOffset value, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["UserId"] = userId,
            ["Application"] = application,
            ["Value"] = value.UtcDateTime,
            ["UpdatedOnUtc"] = value.UtcDateTime
        };

        var updated = await _executor.ExecuteAsync(updateSql, parameters, cancellationToken: cancellationToken);
        if (updated > 0) return;

        try
        {
            await _executor.ExecuteAsync(insertSql, parameters, cancellationToken: cancellationToken);
        }
        catch (DatabaseException)
        {
            var retried = await _executor.ExecuteAsync(updateSql, parameters, cancellationToken: cancellationToken);
            if (retried == 0) throw;
        }
    }

    private static Dictionary<string, object?> BuildRecipientParameters(string application, NotificationRecipient recipient, DateTimeOffset? since)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["Application"] = application,
            ["UserId"] = recipient.UserId
        };

        if (since.HasValue) parameters["Since"] = since.Value.UtcDateTime;

        for (var i = 0; i < recipient.GroupCodes.Count; i++)
            parameters[NotificationSqlBuilder.GroupParameterKey(i)] = recipient.GroupCodes[i];

        return parameters;
    }

    private static NotificationRecord ToRecord(NotificationRow row) => new()
    {
        Id = row.Id,
        Application = row.Application,
        EntityType = row.EntityType,
        EntityId = row.EntityId,
        EventType = row.EventType,
        Title = row.Title,
        Message = row.Message,
        Data = row.Data,
        CreatedBy = row.CreatedBy,
        CreatedOnUtc = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedOnUtc, DateTimeKind.Utc))
    };

    private static NotificationUserState ToState(NotificationUserStateRow row) => new()
    {
        UserId = row.UserId,
        Application = row.Application,
        LastReadOnUtc = row.LastReadOnUtc is { } read ? new DateTimeOffset(DateTime.SpecifyKind(read, DateTimeKind.Utc)) : null,
        LastSyncedOnUtc = row.LastSyncedOnUtc is { } synced ? new DateTimeOffset(DateTime.SpecifyKind(synced, DateTimeKind.Utc)) : null,
        UpdatedOnUtc = new DateTimeOffset(DateTime.SpecifyKind(row.UpdatedOnUtc, DateTimeKind.Utc))
    };
}
