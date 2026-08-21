using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Playground.Metadata;

namespace APIPlatform.Playground.Defaults;

/// <summary>
/// Application-supplied <see cref="IEntityDefaultValueProvider"/> for Employee — CreatedOn/
/// ModifiedOn are engineering-only timestamps, applied generically by CrudEngine's
/// ContextEnrichmentStage before validation runs. Must be registered before AddCrudEngine() so
/// it wins over the platform's NoOpEntityDefaultValueProvider (TryAddSingleton).
/// </summary>
public sealed class EmployeeDefaultValueProvider : IEntityDefaultValueProvider
{
    private static readonly EntityDefaultValueConfig EmployeeConfig = new()
    {
        EntityName = EmployeeEntityDefinitionProvider.EntityName,
        Bindings = new List<DefaultValueBinding>
        {
            new() { FieldName = nameof(Models.Employee.CreatedOn), Kind = DefaultValueKind.UtcNowOnCreate },
            new() { FieldName = nameof(Models.Employee.ModifiedOn), Kind = DefaultValueKind.UtcNowOnUpdate }
        }
    };

    public EntityDefaultValueConfig? TryGetConfig(string entityName) =>
        string.Equals(entityName, EmployeeEntityDefinitionProvider.EntityName, StringComparison.OrdinalIgnoreCase)
            ? EmployeeConfig
            : null;
}
