using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Interfaces;

/// <summary>
/// Runs a <see cref="DynamicQueryRequest"/> and returns raw rows. This is the "engine only
/// processes a description and hands back a result" contract (Req: zero hardcoded table/column
/// names anywhere in the platform) — there is no per-table repository or controller method behind
/// it, so a new consuming app never adds platform code just to read a differently-shaped table.
/// Table/column identifiers are supplied by the caller and are therefore validated (not trusted
/// the way EntityDefinition-driven CrudEngine reads are); see DynamicQueryService for the rules.
/// </summary>
public interface IDynamicQueryService
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        DynamicQueryRequest request,
        CancellationToken cancellationToken = default);
}
