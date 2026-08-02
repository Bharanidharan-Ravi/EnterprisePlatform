using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;

namespace APIPlatform.CrudEngine.Services;

/// <summary>Safe default so AddCrudEngine() works out of the box with no multi-result operations
/// configured. Apps supply their own IMultiResultOperationProvider to enable that feature.</summary>
public sealed class NoOpMultiResultOperationProvider : IMultiResultOperationProvider
{
    public MultiResultOperationConfig? TryGetConfig(string operationKey) => null;
}
