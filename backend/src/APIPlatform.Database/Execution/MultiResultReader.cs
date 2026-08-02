using Dapper;

namespace APIPlatform.Data.Execution;

/// <summary>Default IMultiResultReader — wraps a Dapper GridReader without exposing it.</summary>
internal sealed class MultiResultReader : IMultiResultReader
{
    private readonly SqlMapper.GridReader _gridReader;
    private readonly IDisposable _connectionOwner;

    internal MultiResultReader(SqlMapper.GridReader gridReader, IDisposable connectionOwner)
    {
        _gridReader = gridReader;
        _connectionOwner = connectionOwner;
    }

    public async Task<IReadOnlyList<T>> ReadAsync<T>()
    {
        var result = await _gridReader.ReadAsync<T>();
        return result.AsList();
    }

    public ValueTask DisposeAsync()
    {
        _gridReader.Dispose();
        _connectionOwner.Dispose();
        return ValueTask.CompletedTask;
    }
}
