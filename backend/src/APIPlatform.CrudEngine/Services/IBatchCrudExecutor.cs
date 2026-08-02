using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Executes a heterogeneous batch of CrudBatchUnit operations — across any number of entities —
/// in parallel, and merges partial success/failure into one CrudBatchResponse. Domain-agnostic
/// counterpart to the app-specific "SyncRepositoryV2" pattern: same parallel/merge shape, but
/// driven entirely by EntityName + CrudOperationType instead of a hardcoded per-app config class.
/// </summary>
public interface IBatchCrudExecutor
{
    Task<CrudBatchResponse> ExecuteAsync(IReadOnlyList<CrudBatchUnit> units, CancellationToken cancellationToken = default);
}
