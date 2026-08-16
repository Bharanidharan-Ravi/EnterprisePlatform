namespace APIPlatform.Notification.Sql.Dialects;

/// <summary>
/// Isolates the SQL-Server-vs-SAP-HANA divergence points for Notification's own SQL (identifier
/// quoting and paging syntax). Deliberately not a reference to
/// <c>APIPlatform.CrudEngine.Sql.Dialects.ISqlDialect</c> — Notification must not depend on
/// CrudEngine, so it carries this same small, proven pattern independently. Public (mirroring
/// CrudEngine's own dialect contracts) because it appears on <c>NotificationRepository</c>'s
/// public constructor.
/// </summary>
public interface INotificationSqlDialect
{
    string QuoteIdentifier(string identifier);

    /// <summary>Appends provider-correct paging syntax to a SELECT statement that already has ORDER BY applied.</summary>
    string ApplyPaging(string orderedSelectSql, int skip, int take);
}
