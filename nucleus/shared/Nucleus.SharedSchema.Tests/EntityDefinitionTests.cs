using Nucleus.SharedSchema.Enums;
using Nucleus.SharedSchema.Models;
using Xunit;

namespace Nucleus.SharedSchema.Tests;

/// <summary>
/// Proves the project builds as a real artifact and its models/enums are usable as the platform's
/// real metadata contract (Phase 1, Section 2/14) — not a full domain test suite, just enough to
/// confirm construction, records' value semantics, and the enum surface CrudEngine depends on.
/// </summary>
public class EntityDefinitionTests
{
    [Fact]
    public void EntityDefinition_CanBeConstructed_WithFieldsAndRelationships()
    {
        var definition = new EntityDefinition
        {
            Name = "Widget",
            DisplayName = "Widget",
            SourceType = FieldSourceType.SqlTable,
            SourceName = "Widgets",
            IsTenantScoped = true,
            Fields = new List<FieldDefinition>
            {
                new()
                {
                    Name = "Id",
                    DataType = FieldDataType.Guid,
                    SourceType = FieldSourceType.SqlTable,
                    IsPrimaryKey = true
                },
                new()
                {
                    Name = "CategoryId",
                    DataType = FieldDataType.Guid,
                    SourceType = FieldSourceType.SqlTable,
                    Validation = new ValidationRuleDefinition { Required = true },
                    UiHint = new UiHintDefinition { InputType = UiInputType.Dropdown, DisplayLabel = "Category" },
                    Permissions = new PermissionRequirement { ReadRoles = new[] { "Viewer" }, WriteRoles = new[] { "Editor" } }
                }
            },
            Relationships = new List<RelationshipDefinition>
            {
                new() { TargetEntityName = "Category", LocalKeyField = "CategoryId", TargetKeyField = "Id" }
            }
        };

        Assert.Equal("Widget", definition.Name);
        Assert.Equal(2, definition.Fields.Count);
        Assert.Single(definition.Relationships);
        Assert.True(definition.Fields[0].IsPrimaryKey);
        Assert.Equal(1, definition.SchemaVersion);
    }

    [Fact]
    public void FieldDefinition_DefaultCollections_AreEmptyNotNull()
    {
        var field = new FieldDefinition
        {
            Name = "Name",
            DataType = FieldDataType.String,
            SourceType = FieldSourceType.SqlTable
        };

        Assert.Null(field.EnumValues);
        Assert.Null(field.Validation);
        Assert.Null(field.UiHint);
        Assert.Null(field.Permissions);
    }

    [Fact]
    public void PermissionRequirement_DefaultRoleLists_AreEmpty()
    {
        var permissions = new PermissionRequirement();

        Assert.Empty(permissions.ReadRoles);
        Assert.Empty(permissions.WriteRoles);
    }

    [Fact]
    public void EntityDefinition_IsARecord_WithValueEquality()
    {
        var fields = new List<FieldDefinition>
        {
            new() { Name = "Id", DataType = FieldDataType.Guid, SourceType = FieldSourceType.SqlTable, IsPrimaryKey = true }
        };

        var a = new EntityDefinition { Name = "Widget", DisplayName = "Widget", SourceType = FieldSourceType.SqlTable, SourceName = "Widgets", Fields = fields };
        var b = new EntityDefinition { Name = "Widget", DisplayName = "Widget", SourceType = FieldSourceType.SqlTable, SourceName = "Widgets", Fields = fields };

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(FieldDataType.String)]
    [InlineData(FieldDataType.Integer)]
    [InlineData(FieldDataType.Decimal)]
    [InlineData(FieldDataType.Boolean)]
    [InlineData(FieldDataType.DateTime)]
    [InlineData(FieldDataType.Date)]
    [InlineData(FieldDataType.Guid)]
    [InlineData(FieldDataType.Enum)]
    public void FieldDataType_AllMembers_AreUsableAsFieldDataType(FieldDataType dataType)
    {
        var field = new FieldDefinition { Name = "F", DataType = dataType, SourceType = FieldSourceType.SqlTable };

        Assert.Equal(dataType, field.DataType);
    }
}
