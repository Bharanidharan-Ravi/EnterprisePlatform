using APIPlatform.CrudEngine.Interfaces;
using Nucleus.SharedSchema.Enums;
using Nucleus.SharedSchema.Models;

namespace APIPlatform.Playground.Metadata;

/// <summary>
/// Application-level <see cref="IEntityDefinitionProvider"/> for the Phase 2 Employee test
/// entity. This is exactly the boundary phase2.md describes: Application -&gt;
/// IEntityDefinitionProvider -&gt; EntityDefinition -&gt; CrudEngine. CrudEngine itself has no
/// knowledge of Employee; it only ever sees the generic EntityDefinition/FieldDefinition shapes
/// from Nucleus.SharedSchema. Property names below must match <see cref="Models.Employee"/>
/// exactly, case-insensitively.
/// </summary>
public sealed class EmployeeEntityDefinitionProvider : IEntityDefinitionProvider
{
    public const string EntityName = "Employee";

    private static readonly EntityDefinition Definition = new()
    {
        Name = EntityName,
        DisplayName = "Employee",
        SourceType = FieldSourceType.SqlTable,
        SourceName = "Employees",
        IsTenantScoped = false,
        Fields = new List<FieldDefinition>
        {
            new()
            {
                Name = "Id",
                DataType = FieldDataType.Guid,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                IsPrimaryKey = true,
                UiHint = new UiHintDefinition
                {
                    InputType = UiInputType.Text,
                    DisplayLabel = "Id",
                    Visible = false
                }
            },
            new()
            {
                Name = "EmployeeCode",
                DataType = FieldDataType.String,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                Validation = new ValidationRuleDefinition { Required = true, MaxLength = 20 },
                UiHint = new UiHintDefinition { InputType = UiInputType.Text, DisplayLabel = "Employee Code", ColumnWidth = 6 }
            },
            new()
            {
                Name = "Name",
                DataType = FieldDataType.String,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                Validation = new ValidationRuleDefinition { Required = true, MaxLength = 200 },
                UiHint = new UiHintDefinition { InputType = UiInputType.Text, DisplayLabel = "Name", ColumnWidth = 6 }
            },
            new()
            {
                Name = "Email",
                DataType = FieldDataType.String,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                Validation = new ValidationRuleDefinition { Required = true, MaxLength = 256 },
                UiHint = new UiHintDefinition { InputType = UiInputType.Text, DisplayLabel = "Email", ColumnWidth = 6 }
            },
            new()
            {
                Name = "Department",
                DataType = FieldDataType.String,
                IsNullable = true,
                SourceType = FieldSourceType.SqlTable,
                UiHint = new UiHintDefinition { InputType = UiInputType.Text, DisplayLabel = "Department", ColumnWidth = 6 }
            },
            new()
            {
                Name = "IsActive",
                DataType = FieldDataType.Boolean,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                DefaultValue = "true",
                UiHint = new UiHintDefinition { InputType = UiInputType.Checkbox, DisplayLabel = "Active", ColumnWidth = 12 }
            },
            new()
            {
                Name = "CreatedOn",
                DataType = FieldDataType.DateTime,
                IsNullable = false,
                SourceType = FieldSourceType.SqlTable,
                UiHint = new UiHintDefinition { InputType = UiInputType.Calendar, DisplayLabel = "Created On", Visible = false }
            },
            new()
            {
                Name = "ModifiedOn",
                DataType = FieldDataType.DateTime,
                IsNullable = true,
                SourceType = FieldSourceType.SqlTable,
                UiHint = new UiHintDefinition { InputType = UiInputType.Calendar, DisplayLabel = "Modified On", Visible = false }
            }
        }
    };

    public EntityDefinition GetDefinition(string entityName)
    {
        if (!string.Equals(entityName, EntityName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"No EntityDefinition registered for '{entityName}'. This test host only defines '{EntityName}'.");

        return Definition;
    }
}
