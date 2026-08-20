using APIPlatform.CrudEngine.Models;
using APIPlatform.CrudEngine.Sql.Builders;
using APIPlatform.CrudEngine.Sql.Dialects;
using Xunit;

namespace APIPlatform.CrudEngine.Tests.Sql.Builders;

public class FilterClauseBuilderTests
{
    [Fact]
    public void Build_NoFilters_ReturnsEmptyFragmentAndOriginalFilters()
    {
        var (fragment, parameters) = new FilterClauseBuilder().Build(new Dictionary<string, object?>());

        Assert.Equal(string.Empty, fragment);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Build_MultipleFilters_JoinsWithAndAndPrefixesParameterNames()
    {
        var (fragment, parameters) = new FilterClauseBuilder().Build(
            new Dictionary<string, object?> { ["Name"] = "Bolt", ["Price"] = 5m });

        Assert.Equal("Name = @Filter_Name AND Price = @Filter_Price", fragment);
        Assert.Equal("Bolt", parameters["Filter_Name"]);
        Assert.Equal(5m, parameters["Filter_Price"]);
    }
}

public class SortClauseBuilderTests
{
    [Fact]
    public void Build_NoSortingRequested_FallsBackToKeyFields()
    {
        var sql = new SortClauseBuilder().Build(Array.Empty<SortSpec>(), new[] { "Id" });

        Assert.Equal("ORDER BY Id", sql);
    }

    [Fact]
    public void Build_DescendingSort_AppendsDesc()
    {
        var sql = new SortClauseBuilder().Build(new[] { new SortSpec("Price", Descending: true) }, new[] { "Id" });

        Assert.Equal("ORDER BY Price DESC", sql);
    }

    [Fact]
    public void Build_AscendingSort_OmitsDirectionKeyword()
    {
        var sql = new SortClauseBuilder().Build(new[] { new SortSpec("Price") }, new[] { "Id" });

        Assert.Equal("ORDER BY Price", sql);
    }
}

public class PagingClauseBuilderTests
{
    [Fact]
    public void Apply_NullPaging_ReturnsSqlUnchanged()
    {
        var sql = new PagingClauseBuilder().Apply("SELECT * FROM Widgets ORDER BY Id", paging: null, new SqlServerDialect());

        Assert.Equal("SELECT * FROM Widgets ORDER BY Id", sql);
    }

    [Fact]
    public void Apply_SqlServerDialect_UsesOffsetFetch()
    {
        var sql = new PagingClauseBuilder().Apply("SELECT * FROM Widgets ORDER BY Id", new PagingSpec(0, 10), new SqlServerDialect());

        Assert.Equal("SELECT * FROM Widgets ORDER BY Id OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", sql);
    }

    [Fact]
    public void Apply_HanaDialect_UsesLimitOffset()
    {
        var sql = new PagingClauseBuilder().Apply("SELECT * FROM Widgets ORDER BY Id", new PagingSpec(0, 10), new HanaDialect());

        Assert.Equal("SELECT * FROM Widgets ORDER BY Id LIMIT 10 OFFSET 0", sql);
    }
}
