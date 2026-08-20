using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Interfaces;

/// <summary>
/// Resolves an EntityDefinition by entity name. Nucleus never hardcodes entity metadata —
/// the consuming/generated app registers an implementation backed by whatever config source
/// it uses (JSON, DB table, appsettings, Nucleus Builder output later). Config is the source
/// of truth (Rule 2), same rationale as <see cref="IEntityOperationBindingProvider"/>.
/// Required for any CRUD operation to run — GenericRepository and IEntityMetadataCache both
/// resolve EntityDefinition through this contract, so AddCrudEngine() does not register a
/// NoOp fallback; the host app must supply one.
/// </summary>
public interface IEntityDefinitionProvider
{
    EntityDefinition GetDefinition(string entityName);
}
