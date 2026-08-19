using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Database.Migration.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

/// <summary>
/// Manual-trigger demonstration of APIPlatform.Database.Migration wired against Playground's own
/// configured database (see DatabaseExtensions.AddAPIPlatformDatabaseMigration and Program.cs) —
/// the "application-configured-database, explicit migration step" shape the migration package's
/// README describes. Nothing here runs on startup; a migration only runs if this endpoint is
/// actually called.
/// </summary>
[ApiController]
[Route("api/database-migration")]
public class DatabaseMigrationController : ControllerBase
{
    private readonly IMigrationRunner _runner;
    private readonly IMigrationHistoryRepository _history;

    public DatabaseMigrationController(IMigrationRunner runner, IMigrationHistoryRepository history)
    {
        _runner = runner;
        _history = history;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(cancellationToken);
        return Ok(new
        {
            Applied = result.Applied,
            Skipped = result.Skipped
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var appliedIds = await _history.GetAppliedMigrationIdsAsync(cancellationToken);
        return Ok(new { AppliedMigrationIds = appliedIds });
    }
}
