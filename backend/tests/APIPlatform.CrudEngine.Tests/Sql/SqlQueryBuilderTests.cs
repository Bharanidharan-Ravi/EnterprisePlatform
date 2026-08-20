using APIPlatform.CrudEngine.Sql;
using APIPlatform.CrudEngine.Tests.TestSupport;
using Xunit;

namespace APIPlatform.CrudEngine.Tests.Sql;

/// <summary>
/// Covers Insert/Update/Delete/SelectByKey SQL generation — internal (see InternalsVisibleTo on
/// APIPlatform.CrudEngine.csproj), provider-agnostic (Section 6: no dialect-specific behavior
/// belongs here), and metadata-driven off EntityDefinition/FieldDefinition.
/// </summary>
public class SqlQueryBuilderTests
{
    [Fact]
    public void SelectByKey_BuildsWhereOnKeyFields()
    {
        var sql = SqlQueryBuilder.SelectByKey(EntityDefinitions.Widget(), new[] { "Id" });

        Assert.Equal("SELECT * FROM Widgets WHERE Id = @Id", sql);
    }

    [Fact]
    public void Insert_IncludesAllNativeFieldsInDeclarationOrder()
    {
        var sql = SqlQueryBuilder.Insert(EntityDefinitions.Widget());

        Assert.Equal("INSERT INTO Widgets (Id, Name, Price) VALUES (@Id, @Name, @Price)", sql);
    }

    [Fact]
    public void Update_ExcludesKeyFieldsFromSetClauseButKeepsThemInWhere()
    {
        var sql = SqlQueryBuilder.Update(EntityDefinitions.Widget(), new[] { "Id" });

        Assert.Equal("UPDATE Widgets SET Name = @Name, Price = @Price WHERE Id = @Id", sql);
    }

    [Fact]
    public void Delete_BuildsWhereOnKeyFields()
    {
        var sql = SqlQueryBuilder.Delete(EntityDefinitions.Widget(), new[] { "Id" });

        Assert.Equal("DELETE FROM Widgets WHERE Id = @Id", sql);
    }

    [Fact]
    public void Update_TenantScopedEntity_AppendsTenantCondition()
    {
        var sql = SqlQueryBuilder.Update(EntityDefinitions.Widget(tenantScoped: true), new[] { "Id" });

        Assert.EndsWith("WHERE Id = @Id AND TenantId = @TenantId", sql);
    }

    [Fact]
    public void SelectByKey_TenantScopedEntity_AppendsTenantCondition()
    {
        var sql = SqlQueryBuilder.SelectByKey(EntityDefinitions.Widget(tenantScoped: true), new[] { "Id" });

        Assert.EndsWith("WHERE Id = @Id AND TenantId = @TenantId", sql);
    }
}
