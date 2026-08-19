using APIPlatform.Notification.Sql;
using APIPlatform.Notification.Sql.Dialects;
using Xunit;

namespace APIPlatform.Notification.Tests.Sql;

/// <summary>
/// Asserts the exact SQL text NotificationSqlBuilder produces, for both dialects, without any
/// database — the same "pure text generation" split CrudEngine's SqlQueryBuilder/QuerySqlBuilder
/// use, which is what makes this level of precision testable at all.
/// </summary>
public class NotificationSqlBuilderTests
{
    [Fact]
    public void InsertNotification_SqlServer_QuotesWithBrackets()
    {
        var sql = NotificationSqlBuilder.InsertNotification(new SqlServerNotificationDialect());

        Assert.Contains("INSERT INTO [Notifications]", sql);
        Assert.Contains("@Id", sql);
        Assert.Contains("@CreatedOnUtc", sql);
    }

    [Fact]
    public void InsertNotification_Hana_QuotesWithDoubleQuotes()
    {
        var sql = NotificationSqlBuilder.InsertNotification(new HanaNotificationDialect());

        Assert.Contains("INSERT INTO \"Notifications\"", sql);
    }

    [Fact]
    public void InsertTarget_ProducesAllFiveColumns()
    {
        var sql = NotificationSqlBuilder.InsertTarget(new SqlServerNotificationDialect());

        Assert.Contains("[NotificationTargets]", sql);
        Assert.Contains("@TargetKind", sql);
        Assert.Contains("@TargetValue", sql);
        Assert.Contains("@IsExclusion", sql);
    }

    [Fact]
    public void RecipientMatch_WithoutGroups_OmitsGroupInClause()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 0, includeSince: false, countOnly: false, skip: 0, take: 20);

        Assert.DoesNotContain("@g0", sql);
        Assert.Contains("TargetKind] = 1", sql); // still matches direct user targeting
    }

    [Fact]
    public void RecipientMatch_WithGroups_BuildsInClauseForEachGroup()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 3, includeSince: false, countOnly: false, skip: 0, take: 20);

        Assert.Contains("@g0", sql);
        Assert.Contains("@g1", sql);
        Assert.Contains("@g2", sql);
        Assert.DoesNotContain("@g3", sql);
    }

    [Fact]
    public void RecipientMatch_IncludesBothExistsAndNotExists()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 1, includeSince: true, countOnly: false, skip: 0, take: 20);

        Assert.Contains("EXISTS", sql);
        Assert.Contains("NOT EXISTS", sql);
        Assert.Contains("@Since", sql);
    }

    /// <summary>
    /// Pins the strict "greater than" boundary: a notification created in the exact same instant
    /// as the read cursor (@Since = LastReadOnUtc) must NOT be treated as unread. Regressing this
    /// to ">=" would make GetUnreadCountAsync/GetNotificationsAsync re-surface notifications the
    /// user has already read as of that exact timestamp.
    /// </summary>
    [Fact]
    public void RecipientMatch_SinceComparison_IsStrictlyGreaterThan()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 0, includeSince: true, countOnly: false, skip: 0, take: 20);

        Assert.Contains("> @Since", sql);
        Assert.DoesNotContain(">= @Since", sql);
    }

    /// <summary>
    /// The group-match branch is an "IN (...)" list inside the same EXISTS semi-join used for
    /// direct user/ALL targeting, never a JOIN against NotificationTargets. A JOIN would fan out
    /// one notification row per matching group (duplicate recipients whenever a user belongs to
    /// more than one targeted group); EXISTS/IN can only ever contribute 0 or 1 row per
    /// notification regardless of how many of the recipient's groups match, so duplicates are
    /// structurally impossible by construction, not by later de-duplication.
    /// </summary>
    [Fact]
    public void RecipientMatch_WithMultipleGroups_UsesExistsInClause_NeverJoin_SoNoDuplicateRows()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 3, includeSince: false, countOnly: false, skip: 0, take: 20);

        Assert.Contains("TargetKind] = 2 AND", sql);
        Assert.Contains("IN (@g0, @g1, @g2)", sql);
        Assert.DoesNotContain("JOIN", sql);
        // Exactly one row template is selected from Notifications ("n") — group matching only
        // ever narrows the EXISTS subquery, it can never multiply the outer row.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(sql, "FROM \\[Notifications\\]"));
    }

    [Fact]
    public void RecipientMatch_CountOnly_UsesCountStarAndNoPaging()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 0, includeSince: false, countOnly: true);

        Assert.StartsWith("SELECT COUNT(*)", sql);
        Assert.DoesNotContain("OFFSET", sql);
        Assert.DoesNotContain("FETCH", sql);
    }

    [Fact]
    public void RecipientMatch_SqlServer_UsesOffsetFetchPaging()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new SqlServerNotificationDialect(), groupCount: 0, includeSince: false, countOnly: false, skip: 10, take: 20);

        Assert.Contains("OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY", sql);
    }

    [Fact]
    public void RecipientMatch_Hana_UsesLimitOffsetPaging()
    {
        var sql = NotificationSqlBuilder.RecipientMatch(new HanaNotificationDialect(), groupCount: 0, includeSince: false, countOnly: false, skip: 10, take: 20);

        Assert.Contains("LIMIT 20 OFFSET 10", sql);
    }

    [Fact]
    public void EntityHistory_FiltersByApplicationEntityTypeAndEntityId()
    {
        var sql = NotificationSqlBuilder.EntityHistory(new SqlServerNotificationDialect(), skip: 0, take: 10);

        Assert.Contains("@Application", sql);
        Assert.Contains("@EntityType", sql);
        Assert.Contains("@EntityId", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void GroupParameterKey_HasNoAtPrefix_ForParameterDictionaryUse()
    {
        Assert.Equal("g0", NotificationSqlBuilder.GroupParameterKey(0));
        Assert.Equal("g7", NotificationSqlBuilder.GroupParameterKey(7));
    }

    [Fact]
    public void UpdateAndInsertLastReadOn_TargetDifferentColumnsThanLastSyncedOn()
    {
        var dialect = new SqlServerNotificationDialect();

        var updateRead = NotificationSqlBuilder.UpdateLastReadOn(dialect);
        var updateSynced = NotificationSqlBuilder.UpdateLastSyncedOn(dialect);

        Assert.Contains("[LastReadOnUtc] = @Value", updateRead);
        Assert.Contains("[LastSyncedOnUtc] = @Value", updateSynced);
        Assert.DoesNotContain("LastSyncedOnUtc", updateRead);
        Assert.DoesNotContain("LastReadOnUtc", updateSynced);
    }
}
