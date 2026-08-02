namespace APIPlatform.CrudEngine.Sql.Dialects;

/// <summary>
/// Isolates the SQL-Server-vs-SAP-HANA divergence points (Req 11) so builders never branch on
/// provider directly. Insert/Update/Delete generation (SqlQueryBuilder) is already ANSI-standard
/// enough to need no dialect awareness; paging syntax is the concrete point of difference.
/// </summary>
public interface ISqlDialect
{
    string Name { get; }
    string QuoteIdentifier(string identifier);

    /// <summary>Appends provider-correct paging syntax to a base (unpaged) SELECT statement that
    /// already has its ORDER BY applied.</summary>
    string ApplyPaging(string orderedSelectSql, int skip, int take);
}
