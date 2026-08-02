namespace APIPlatform.CrudEngine.Sql.Dialects;

/// <summary>T-SQL dialect — OFFSET/FETCH paging (SQL Server 2012+).</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    public string Name => "SqlServer";
    public string QuoteIdentifier(string identifier) => $"[{identifier}]";
    public string ApplyPaging(string orderedSelectSql, int skip, int take) =>
        $"{orderedSelectSql} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
}
