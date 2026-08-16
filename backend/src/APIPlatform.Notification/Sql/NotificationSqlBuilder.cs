using APIPlatform.Notification.Sql.Dialects;

namespace APIPlatform.Notification.Sql;

/// <summary>
/// Pure SQL-text generation for Notification's three tables. Kept free of any
/// IDatabaseExecutor/Dapper dependency so the generated SQL can be asserted directly in unit
/// tests without a live database (mirrors CrudEngine's SqlQueryBuilder/QuerySqlBuilder split
/// between "build the text" and "execute it").
/// </summary>
internal static class NotificationSqlBuilder
{
    private const string NotificationsTable = "Notifications";
    private const string TargetsTable = "NotificationTargets";
    private const string UserStatesTable = "NotificationUserStates";

    private static readonly string[] NotificationColumns =
        ["Id", "Application", "EntityType", "EntityId", "EventType", "Title", "Message", "Data", "CreatedBy", "CreatedOnUtc"];

    public static string InsertNotification(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(NotificationsTable);
        var columns = string.Join(", ", NotificationColumns.Select(dialect.QuoteIdentifier));
        var parameters = string.Join(", ", NotificationColumns.Select(c => "@" + c));
        return $"INSERT INTO {table} ({columns}) VALUES ({parameters})";
    }

    public static string InsertTarget(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(TargetsTable);
        var columns = string.Join(", ", new[] { "Id", "NotificationId", "TargetKind", "TargetValue", "IsExclusion" }.Select(dialect.QuoteIdentifier));
        return $"INSERT INTO {table} ({columns}) VALUES (@Id, @NotificationId, @TargetKind, @TargetValue, @IsExclusion)";
    }

    /// <summary>
    /// Builds the recipient-matching query shared by list and count reads: notifications for
    /// <paramref name="application"/> targeted at the recipient (directly, via ALL, or via one of
    /// their groups) and not excluded, optionally only those created after <c>@Since</c>.
    /// A single indexed EXISTS/NOT EXISTS pair against NotificationTargets — no per-notification
    /// round trip and no per-group round trip, since group codes are passed in as one IN list.
    /// </summary>
    public static string RecipientMatch(INotificationSqlDialect dialect, int groupCount, bool includeSince, bool countOnly, int skip = 0, int take = 0)
    {
        var notifications = dialect.QuoteIdentifier(NotificationsTable);
        var targets = dialect.QuoteIdentifier(TargetsTable);
        var n = dialect.QuoteIdentifier("n");
        var t = dialect.QuoteIdentifier("t");

        var groupIn = groupCount > 0
            ? $" OR ({t}.{dialect.QuoteIdentifier("TargetKind")} = 2 AND {t}.{dialect.QuoteIdentifier("TargetValue")} IN ({GroupParameterList(groupCount)}))"
            : string.Empty;

        var includeMatch =
            $"EXISTS (SELECT 1 FROM {targets} {t} WHERE {t}.{dialect.QuoteIdentifier("NotificationId")} = {n}.{dialect.QuoteIdentifier("Id")} " +
            $"AND {t}.{dialect.QuoteIdentifier("IsExclusion")} = 0 " +
            $"AND ({t}.{dialect.QuoteIdentifier("TargetKind")} = 0 " +
            $"OR ({t}.{dialect.QuoteIdentifier("TargetKind")} = 1 AND {t}.{dialect.QuoteIdentifier("TargetValue")} = @UserId){groupIn}))";

        var excludeMatch =
            $"NOT EXISTS (SELECT 1 FROM {targets} {t} WHERE {t}.{dialect.QuoteIdentifier("NotificationId")} = {n}.{dialect.QuoteIdentifier("Id")} " +
            $"AND {t}.{dialect.QuoteIdentifier("IsExclusion")} = 1 " +
            $"AND (({t}.{dialect.QuoteIdentifier("TargetKind")} = 1 AND {t}.{dialect.QuoteIdentifier("TargetValue")} = @UserId){groupIn}))";

        var since = includeSince ? $" AND {n}.{dialect.QuoteIdentifier("CreatedOnUtc")} > @Since" : string.Empty;

        var where =
            $"{n}.{dialect.QuoteIdentifier("Application")} = @Application{since} AND {includeMatch} AND {excludeMatch}";

        if (countOnly)
            return $"SELECT COUNT(*) FROM {notifications} {n} WHERE {where}";

        var select = string.Join(", ", NotificationColumns.Select(c => $"{n}.{dialect.QuoteIdentifier(c)}"));
        var orderedSql = $"SELECT {select} FROM {notifications} {n} WHERE {where} ORDER BY {n}.{dialect.QuoteIdentifier("CreatedOnUtc")} DESC";
        return dialect.ApplyPaging(orderedSql, skip, take);
    }

    public static string EntityHistory(INotificationSqlDialect dialect, int skip, int take)
    {
        var table = dialect.QuoteIdentifier(NotificationsTable);
        var columns = string.Join(", ", NotificationColumns.Select(dialect.QuoteIdentifier));
        var orderedSql =
            $"SELECT {columns} FROM {table} " +
            $"WHERE {dialect.QuoteIdentifier("Application")} = @Application " +
            $"AND {dialect.QuoteIdentifier("EntityType")} = @EntityType AND {dialect.QuoteIdentifier("EntityId")} = @EntityId " +
            $"ORDER BY {dialect.QuoteIdentifier("CreatedOnUtc")} DESC";
        return dialect.ApplyPaging(orderedSql, skip, take);
    }

    public static string GetUserState(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(UserStatesTable);
        var columns = string.Join(", ", new[] { "UserId", "Application", "LastReadOnUtc", "LastSyncedOnUtc", "UpdatedOnUtc" }.Select(dialect.QuoteIdentifier));
        return $"SELECT {columns} FROM {table} WHERE {dialect.QuoteIdentifier("UserId")} = @UserId AND {dialect.QuoteIdentifier("Application")} = @Application";
    }

    public static string UpdateLastReadOn(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(UserStatesTable);
        return $"UPDATE {table} SET {dialect.QuoteIdentifier("LastReadOnUtc")} = @Value, {dialect.QuoteIdentifier("UpdatedOnUtc")} = @UpdatedOnUtc " +
               $"WHERE {dialect.QuoteIdentifier("UserId")} = @UserId AND {dialect.QuoteIdentifier("Application")} = @Application";
    }

    public static string InsertUserStateWithLastReadOn(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(UserStatesTable);
        var columns = string.Join(", ", new[] { "UserId", "Application", "LastReadOnUtc", "LastSyncedOnUtc", "UpdatedOnUtc" }.Select(dialect.QuoteIdentifier));
        return $"INSERT INTO {table} ({columns}) VALUES (@UserId, @Application, @Value, NULL, @UpdatedOnUtc)";
    }

    public static string UpdateLastSyncedOn(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(UserStatesTable);
        return $"UPDATE {table} SET {dialect.QuoteIdentifier("LastSyncedOnUtc")} = @Value, {dialect.QuoteIdentifier("UpdatedOnUtc")} = @UpdatedOnUtc " +
               $"WHERE {dialect.QuoteIdentifier("UserId")} = @UserId AND {dialect.QuoteIdentifier("Application")} = @Application";
    }

    public static string InsertUserStateWithLastSyncedOn(INotificationSqlDialect dialect)
    {
        var table = dialect.QuoteIdentifier(UserStatesTable);
        var columns = string.Join(", ", new[] { "UserId", "Application", "LastReadOnUtc", "LastSyncedOnUtc", "UpdatedOnUtc" }.Select(dialect.QuoteIdentifier));
        return $"INSERT INTO {table} ({columns}) VALUES (@UserId, @Application, NULL, @Value, @UpdatedOnUtc)";
    }

    /// <summary>Parameter dictionary key (no "@") for the group code at <paramref name="index"/> — use this to populate the parameters dictionary.</summary>
    public static string GroupParameterKey(int index) => $"g{index}";

    /// <summary>Parameter marker (with "@") for the group code at <paramref name="index"/> — matches <see cref="GroupParameterKey"/>, used only when embedding directly in SQL text.</summary>
    private static string GroupParameterName(int index) => $"@{GroupParameterKey(index)}";

    private static string GroupParameterList(int groupCount) =>
        string.Join(", ", Enumerable.Range(0, groupCount).Select(GroupParameterName));
}
