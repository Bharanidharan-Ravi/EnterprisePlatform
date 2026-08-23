using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Exceptions;
using APIPlatform.Playground.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

/// <summary>
/// One generic endpoint, backed by <see cref="IDynamicQueryService"/>, instead of one hand-written
/// endpoint per table shape. Replaces the pattern the old IQS LoginController's "reloaduserdata"
/// action used (a query the engine had baked in for one specific table/column set): here the
/// caller's request body names the table, the columns, and the filter — the engine only ever
/// processes that description and returns rows, so it never hardcodes anything about "user",
/// "login", or any other domain concept. The same endpoint reloads user data, looks up a
/// reference table, or backs any other "fetch a filtered row/page" need a generated app has.
///
/// <para><b>Authorized.</b> Unlike table/column names baked into developer-authored code (trusted
/// EntityDefinition config, per CrudEngine's SqlQueryBuilder), the table/columns/filters here come
/// from the caller, so this must not be reachable anonymously — anyone able to call it can read
/// any table's columns. A real login-time lookup (before a token exists) belongs behind
/// IIdentityResolver instead, not this endpoint.</para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/data")]
public class DynamicDataController : ControllerBase
{
    private readonly IDynamicQueryService _dynamicQuery;

    public DynamicDataController(IDynamicQueryService dynamicQuery)
    {
        _dynamicQuery = dynamicQuery;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] DynamicQueryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _dynamicQuery.QueryAsync(request, cancellationToken);
            return Ok(ApiEnvelope.Ok(rows));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiEnvelope.Fail(
                "validation_failed",
                "The query request failed validation.",
                ex.Errors.ToDictionary(e => e.Key, e => e.Value)));
        }
    }
}
