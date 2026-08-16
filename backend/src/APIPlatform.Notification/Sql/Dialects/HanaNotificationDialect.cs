namespace APIPlatform.Notification.Sql.Dialects;

/// <summary>SAP HANA dialect — LIMIT/OFFSET paging.</summary>
internal sealed class HanaNotificationDialect : INotificationSqlDialect
{
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public string ApplyPaging(string orderedSelectSql, int skip, int take) =>
        $"{orderedSelectSql} LIMIT {take} OFFSET {skip}";
}
