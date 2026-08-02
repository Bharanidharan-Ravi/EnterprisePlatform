namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Runs a config-registered multi-result-set stored procedure (see MultiResultOperationConfig)
/// and returns each result set keyed by its declared ResultKey. Domain-agnostic equivalent of
/// SyncExecutionService.ExecuteLocalMultiAsync — usable for any "one call, N tables" operation,
/// not just workflow/project templates.
/// </summary>
public interface IMultiResultQueryService
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<object>>> ExecuteAsync(
        string operationKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}
