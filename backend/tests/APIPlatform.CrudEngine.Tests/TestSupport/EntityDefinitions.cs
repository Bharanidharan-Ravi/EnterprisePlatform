using Nucleus.SharedSchema.Enums;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Tests.TestSupport;

/// <summary>Sample EntityDefinition fixtures shared across CrudEngine tests — a minimal SQL-table
/// entity with a Guid primary key plus two ordinary fields.</summary>
internal static class EntityDefinitions
{
    public static EntityDefinition Widget(bool tenantScoped = false) => new()
    {
        Name = "Widget",
        DisplayName = "Widget",
        SourceType = FieldSourceType.SqlTable,
        SourceName = "Widgets",
        IsTenantScoped = tenantScoped,
        Fields = new List<FieldDefinition>
        {
            new() { Name = "Id", DataType = FieldDataType.Guid, SourceType = FieldSourceType.SqlTable, IsPrimaryKey = true },
            new() { Name = "Name", DataType = FieldDataType.String, SourceType = FieldSourceType.SqlTable },
            new() { Name = "Price", DataType = FieldDataType.Decimal, SourceType = FieldSourceType.SqlTable },
        }
    };
}
