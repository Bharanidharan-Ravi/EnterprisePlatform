using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql;
using APIPlatform.CrudEngine.Sql.Builders;
using APIPlatform.CrudEngine.Sql.Dialects;
using APIPlatform.CrudEngine.Tests.TestSupport;
using Xunit;

namespace APIPlatform.CrudEngine.Tests.Sql;

/// <summary>
/// Covers the List() SQL generation path (metadata -> filter -> sort -> provider paging) for
/// both supported providers, per Phase 1 Section 7/14: metadata resolution feeds EntityDefinition
/// in, filtering/sorting/paging compose the same way regardless of provider, and only the paging
/// fragment itself varies by dialect.
/// </summary>
public class QuerySqlBuilderTests
{
    private static readonly QuerySqlBuilder Builder =
        new(new FilterClauseBuilder(), new SortClauseBuilder(), new PagingClauseBuilder());

    [Fact]
    public void Build_SqlServerDialect_UsesOffsetFetchPagingAndFilterAndSort()
    {
        var def = EntityDefinitions.Widget();
        var plan = new OperationPlan
        {
            Operation = CrudOperationType.List,
            Filters = new Dictionary<string, object?> { ["Name"] = "Bolt" },
            Sorting = new[] { new SortSpec("Price", Descending: true) },
            Paging = new PagingSpec(Skip: 10, Take: 20)
        };

        var (sql, parameters) = Builder.Build(def, plan, new[] { "Id" }, new SqlServerDialect(), tenantId: null);

        Assert.Contains("SELECT * FROM Widgets", sql);
        Assert.Contains("WHERE Name = @Filter_Name", sql);
        Assert.Contains("ORDER BY Price DESC", sql);
        Assert.Contains("OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY", sql);
        Assert.Equal("Bolt", parameters["Filter_Name"]);
    }

    [Fact]
    public void Build_HanaDialect_UsesLimitOffsetPaging()
    {
        var def = EntityDefinitions.Widget();
        var plan = new OperationPlan { Operation = CrudOperationType.List, Paging = new PagingSpec(Skip: 5, Take: 15) };

        var (sql, _) = Builder.Build(def, plan, new[] { "Id" }, new HanaDialect(), tenantId: null);

        Assert.Contains("LIMIT 15 OFFSET 5", sql);
        Assert.DoesNotContain("OFFSET 5 ROWS", sql);
    }

    [Fact]
    public void Build_NoSortingRequested_DefaultsOrderByToKeyFields()
    {
        var def = EntityDefinitions.Widget();
        var plan = new OperationPlan { Operation = CrudOperationType.List };

        var (sql, _) = Builder.Build(def, plan, new[] { "Id" }, new SqlServerDialect(), tenantId: null);

        Assert.Contains("ORDER BY Id", sql);
    }

    [Fact]
    public void Build_NoFilters_OmitsWhereClause()
    {
        var def = EntityDefinitions.Widget();
        var plan = new OperationPlan { Operation = CrudOperationType.List };

        var (sql, _) = Builder.Build(def, plan, new[] { "Id" }, new SqlServerDialect(), tenantId: null);

        Assert.DoesNotContain("WHERE", sql);
    }

    [Fact]
    public void Build_TenantScopedEntityWithFilter_CombinesTenantAndFilterWithAnd()
    {
        var def = EntityDefinitions.Widget(tenantScoped: true);
        var plan = new OperationPlan
        {
            Operation = CrudOperationType.List,
            Filters = new Dictionary<string, object?> { ["Name"] = "Bolt" }
        };

        var (sql, parameters) = Builder.Build(def, plan, new[] { "Id" }, new SqlServerDialect(), tenantId: "tenant-1");

        Assert.Contains("WHERE TenantId = @TenantId AND Name = @Filter_Name", sql);
        Assert.Equal("tenant-1", parameters["TenantId"]);
    }
}
