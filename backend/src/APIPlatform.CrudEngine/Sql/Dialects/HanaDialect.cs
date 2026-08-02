namespace APIPlatform.CrudEngine.Sql.Dialects;

/// <summary>SAP HANA dialect — LIMIT/OFFSET paging.</summary>
public sealed class HanaDialect : ISqlDialect
{
    public string Name => "Hana";
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";
    public string ApplyPaging(string orderedSelectSql, int skip, int take) =>
        $"{orderedSelectSql} LIMIT {take} OFFSET {skip}";
}
