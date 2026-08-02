namespace APIPlatform.Data.Execution;

/// <summary>
/// Reads successive result sets from a single multi-result query, without exposing the
/// underlying Dapper GridReader (or any other provider-specific reader type) to consumers.
/// </summary>
public interface IMultiResultReader : IAsyncDisposable
{
    /// <summary>Reads the next result set as a list of T. Must be called in the same order the result sets were produced.</summary>
    Task<IReadOnlyList<T>> ReadAsync<T>();
}
