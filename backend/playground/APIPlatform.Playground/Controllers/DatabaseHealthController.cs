using System;
using System.Diagnostics;
using System.Threading.Tasks;
using APIPlatform.Data.Connections;
using APIPlatform.Data.Execution;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

[ApiController]
[Route("api/database")]
public class DatabaseHealthController : ControllerBase
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IDatabaseExecutor _executor;

    public DatabaseHealthController(
        IDatabaseConnectionFactory connectionFactory,
        IDatabaseExecutor executor)
    {
        _connectionFactory = connectionFactory;
        _executor = executor;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        
        return Ok(new
        {
            Connected = true,
            Provider = "Database",
            Server = connection.Database,
            ConnectionState = connection.State.ToString()
        });
    }

    [HttpGet("scalar")]
    public async Task<IActionResult> Scalar()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _executor.ExecuteScalarAsync<int>("SELECT 1");
        stopwatch.Stop();

        return Ok(new
        {
            Success = true,
            Value = result,
            ExecutionTime = stopwatch.ElapsedMilliseconds + "ms"
        });
    }

    [HttpGet("info")]
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public async Task<IActionResult> Info()
    {
        var dbName = await _executor.ExecuteScalarAsync<string>("SELECT DB_NAME()");
        var version = await _executor.ExecuteScalarAsync<string>("SELECT @@VERSION");

        return Ok(new
        {
            DatabaseName = dbName,
            Provider = "SQL Server",
            ServerVersion = version
        });
    }
}
