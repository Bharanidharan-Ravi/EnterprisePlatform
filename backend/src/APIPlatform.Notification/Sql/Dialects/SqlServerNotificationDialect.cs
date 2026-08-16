namespace APIPlatform.Notification.Sql.Dialects;

/// <summary>T-SQL dialect — OFFSET/FETCH paging (SQL Server 2012+).</summary>
internal sealed class SqlServerNotificationDialect : INotificationSqlDialect
{
    public string QuoteIdentifier(string identifier) => $"[{identifier}]";

    public string ApplyPaging(string orderedSelectSql, int skip, int take) =>
        $"{orderedSelectSql} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
}
