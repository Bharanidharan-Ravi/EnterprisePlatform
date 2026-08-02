using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql.Dialects;

namespace APIPlatform.CrudEngine.Sql.Builders;

/// <summary>Applies provider-correct paging on top of an already-ordered SELECT — thin wrapper
/// so the query builder doesn't need to know dialect details either.</summary>
public interface IPagingClauseBuilder
{
    string Apply(string orderedSelectSql, PagingSpec? paging, ISqlDialect dialect);
}

public sealed class PagingClauseBuilder : IPagingClauseBuilder
{
    public string Apply(string orderedSelectSql, PagingSpec? paging, ISqlDialect dialect) =>
        paging is null ? orderedSelectSql : dialect.ApplyPaging(orderedSelectSql, paging.Skip, paging.Take);
}
