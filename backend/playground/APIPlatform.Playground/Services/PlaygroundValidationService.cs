using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Data.Execution;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Playground.Models;

namespace APIPlatform.Playground.Services;

public sealed class PlaygroundValidationService
{
    private readonly IDatabaseExecutor _executor;

    public PlaygroundValidationService(IDatabaseExecutor executor)
    {
        _executor = executor;
    }

    public async Task InitializeTableAsync(CancellationToken cancellationToken = default)
    {
        await _executor.ExecuteAsync(PlaygroundSqlScripts.CreateTableScript, cancellationToken: cancellationToken);
    }

    public async Task<PlaygroundRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _executor.QueryFirstOrDefaultAsync<PlaygroundRecord>(
            PlaygroundSqlScripts.GetByIdScript, 
            new Dictionary<string, object?> { { "Id", id } }, 
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PlaygroundRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _executor.QueryAsync<PlaygroundRecord>(
            PlaygroundSqlScripts.GetAllScript, 
            cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(PlaygroundRecord record, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Id", record.Id },
            { "Name", record.Name },
            { "Value", record.Value },
            { "CreatedOn", record.CreatedOn }
        };
        await _executor.ExecuteAsync(
            PlaygroundSqlScripts.InsertScript, 
            parameters, 
            cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(PlaygroundRecord record, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            { "Id", record.Id },
            { "Name", record.Name },
            { "Value", record.Value },
            { "CreatedOn", record.CreatedOn }
        };
        await _executor.ExecuteAsync(
            PlaygroundSqlScripts.UpdateScript, 
            parameters, 
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _executor.ExecuteAsync(
            PlaygroundSqlScripts.DeleteScript, 
            new Dictionary<string, object?> { { "Id", id } }, 
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteScalarAsync<int>(
            PlaygroundSqlScripts.CountScript, 
            cancellationToken: cancellationToken);
    }
}
