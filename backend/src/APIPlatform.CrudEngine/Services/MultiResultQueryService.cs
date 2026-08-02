using APIPlatform.CrudEngine.Adapters;
using APIPlatform.CrudEngine.Interfaces;

namespace APIPlatform.CrudEngine.Services;

/// <summary>Default IMultiResultQueryService — resolves config via IMultiResultOperationProvider,
/// resolves each result set's CLR type via IEntityTypeRegistry, executes through IProcedurePort.</summary>
public sealed class MultiResultQueryService : IMultiResultQueryService
{
    private readonly IProcedurePort _procedures;
    private readonly IMultiResultOperationProvider _configs;
    private readonly IEntityTypeRegistry _typeRegistry;

    public MultiResultQueryService(IProcedurePort procedures, IMultiResultOperationProvider configs, IEntityTypeRegistry typeRegistry)
    {
        _procedures = procedures;
        _configs = configs;
        _typeRegistry = typeRegistry;
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<object>>> ExecuteAsync(
        string operationKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var config = _configs.TryGetConfig(operationKey)
            ?? throw new InvalidOperationException($"No MultiResultOperationConfig registered for key '{operationKey}'.");

        var resultSets = config.Results
            .Select(r => (r.ResultKey, _typeRegistry.Resolve(r.EntityName)))
            .ToList();

        return _procedures.QueryMultipleAsync(
            config.ProcedureName,
            parameters ?? new Dictionary<string, object?>(),
            resultSets,
            cancellationToken);
    }
}
