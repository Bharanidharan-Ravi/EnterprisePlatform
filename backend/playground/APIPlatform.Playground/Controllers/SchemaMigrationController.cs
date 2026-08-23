using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APIPlatform.Database.Migration.Schema.Abstractions;
using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Templates;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

/// <summary>
/// One endpoint set for every table, instead of one migration class per table. Post a definition
/// naming a predefined table (<c>login</c>, <c>audit</c>, <c>notification</c>, …) to create it
/// with its standard columns; name anything else and the table is built from the fields in the
/// same body. Extra fields are additive either way, so a predefined table can carry app-specific
/// columns without any new platform code.
///
/// <para>All schema work lives in APIPlatform.Database.Migration's
/// <see cref="ISchemaMigrationService"/> — this controller only maps request bodies onto it and
/// its results onto status codes, so a real app gets the same behaviour by calling the same
/// service.</para>
///
/// <para><b>Privileged.</b> These endpoints create and drop tables. Playground leaves them open
/// because it is a local test harness; anywhere else they belong behind administrator
/// authorization.</para>
/// </summary>
[ApiController]
[Route("api/schema")]
public class SchemaMigrationController : ControllerBase
{
    private readonly ISchemaMigrationService _schema;

    public SchemaMigrationController(ISchemaMigrationService schema)
    {
        _schema = schema;
    }

    /// <summary>The predefined tables available by name, and the columns each one brings.</summary>
    [HttpGet("templates")]
    public IActionResult Templates() =>
        Ok(TableTemplateCatalog.Templates.Select(t => new
        {
            t.Key,
            t.TableName,
            t.Description,
            Fields = t.Fields.Select(f => new { f.Name, f.Type, f.MaxLength, f.Nullable, f.Unique, f.Indexed })
        }));

    /// <summary>Creates a table. 409 if it already exists — nothing is ever altered here.</summary>
    [HttpPost("tables")]
    public async Task<IActionResult> Create([FromBody] TableDefinition definition, CancellationToken cancellationToken) =>
        ToResponse(await _schema.CreateTableAsync(definition, cancellationToken));

    /// <summary>Adds any requested columns the table does not already have. Additive only.</summary>
    [HttpPut("tables")]
    public async Task<IActionResult> Update([FromBody] TableDefinition definition, CancellationToken cancellationToken) =>
        ToResponse(await _schema.UpdateTableAsync(definition, cancellationToken));

    /// <summary>
    /// Drops a table and every row in it. Irreversible, so it requires an explicit
    /// <c>?confirm=true</c> — a bare DELETE is refused rather than treated as intent.
    /// </summary>
    [HttpDelete("tables/{tableName}")]
    public async Task<IActionResult> Delete(string tableName, [FromQuery] bool confirm, CancellationToken cancellationToken)
    {
        if (!confirm)
        {
            return BadRequest(new
            {
                Table = tableName,
                Message = $"Dropping '{tableName}' deletes the table and all of its rows and cannot be undone. " +
                          "Re-send with ?confirm=true if that is intended."
            });
        }

        return ToResponse(await _schema.DeleteTableAsync(tableName, cancellationToken));
    }

    /// <summary>Whether a table currently exists.</summary>
    [HttpGet("tables/{tableName}")]
    public async Task<IActionResult> Exists(string tableName, CancellationToken cancellationToken) =>
        Ok(new { Table = tableName, Exists = await _schema.TableExistsAsync(tableName, cancellationToken) });

    private IActionResult ToResponse(SchemaOperationResult result) => result.Status switch
    {
        SchemaOperationStatus.Invalid => BadRequest(result),
        SchemaOperationStatus.Conflict => Conflict(result),
        _ => Ok(result)
    };
}
