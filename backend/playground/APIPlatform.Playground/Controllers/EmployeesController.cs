using APIPlatform.CrudEngine.Engine;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Playground.Metadata;
using APIPlatform.Playground.Models;
using APIPlatform.Rbac.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

/// <summary>
/// Phase 2 proof controller: the HTTP layer belongs here (the application); every CRUD
/// operation, filter, sort, page, and validation below is executed by the generic
/// ICrudEngine&lt;Employee&gt; -&gt; CrudEngine&lt;Employee&gt; -&gt; SharedSchema -&gt; GenericRepository -&gt;
/// Dapper -&gt; SQL Server pipeline. No Employee-specific SQL is written here or anywhere in the
/// platform. Authorization uses ICrudAuthorizationService directly — Rbac has no ASP.NET Core
/// dependency, so no policy/handler plumbing is required to prove allow/deny (phase2.md 22).
/// </summary>
[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private const string EntityKey = "employee"; // lowercase, matches EmployeeModuleInitializationService's seeded grant keys

    private readonly ICrudEngine<Employee> _crud;
    private readonly ICrudAuthorizationService _authorization;

    public EmployeesController(ICrudEngine<Employee> crud, ICrudAuthorizationService authorization)
    {
        _crud = crud;
        _authorization = authorization;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var authz = await _authorization.AuthorizeAsync(EntityKey, "read", cancellationToken);
        if (!authz.Allowed) return Forbidden(authz.Reason);

        var employee = await _crud.GetAsync(new EntityKeyValues { ["Id"] = id }, cancellationToken);
        if (employee is null) return NotFound(ApiEnvelope.Fail("employee_not_found", $"No employee with id '{id}'."));

        return Ok(ApiEnvelope.Ok(employee));
    }

    /// <summary>
    /// List, with optional equality filter (employeeCode), sort (e.g. "Name" or "-Name" for
    /// descending), and paging (page/pageSize) — all passed straight into ICrudEngine.ListAsync
    /// so the generic QuerySqlBuilder path (not GenericRepository's plain SelectAll) is actually
    /// exercised, per phase2.md 16-18.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? employeeCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var authz = await _authorization.AuthorizeAsync(EntityKey, "read", cancellationToken);
        if (!authz.Allowed) return Forbidden(authz.Reason);

        IReadOnlyDictionary<string, object?>? filters = null;
        if (!string.IsNullOrWhiteSpace(employeeCode))
            filters = new Dictionary<string, object?> { ["EmployeeCode"] = employeeCode };

        IReadOnlyList<SortSpec>? sorting = null;
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var descending = sort.StartsWith('-');
            var fieldName = descending ? sort[1..] : sort;
            sorting = new List<SortSpec> { new(fieldName, descending) };
        }

        PagingSpec? paging = null;
        if (page is > 0 && pageSize is > 0)
            paging = new PagingSpec((page.Value - 1) * pageSize.Value, pageSize.Value);

        var employees = await _crud.ListAsync(filters, sorting, paging, cancellationToken);
        return Ok(ApiEnvelope.Ok(employees));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Employee employee, CancellationToken cancellationToken)
    {
        var authz = await _authorization.AuthorizeAsync(EntityKey, "create", cancellationToken);
        if (!authz.Allowed) return Forbidden(authz.Reason);

        if (employee.Id == Guid.Empty)
            employee.Id = Guid.NewGuid();

        var result = await _crud.InsertAsync(employee, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(ApiEnvelope.Fail("validation_failed", "Employee failed validation.", ToFieldErrors(result.Errors)));

        return CreatedAtAction(nameof(Get), new { id = employee.Id }, ApiEnvelope.Ok(result.Value));
    }

    /// <summary>
    /// Loads the existing row first and applies only the mutable fields from the request body
    /// onto it, preserving Id/CreatedOn — GenericRepository's UPDATE statement sets every
    /// non-key native field (including CreatedOn), so passing the raw request body straight
    /// through would silently blank CreatedOn on every edit (phase2.md 19: "no unrelated
    /// fields are accidentally overwritten"). ModifiedOn is set by CrudEngine's
    /// IEntityDefaultValueProvider (UtcNowOnUpdate), not here.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Employee employee, CancellationToken cancellationToken)
    {
        var authz = await _authorization.AuthorizeAsync(EntityKey, "update", cancellationToken);
        if (!authz.Allowed) return Forbidden(authz.Reason);

        var existing = await _crud.GetAsync(new EntityKeyValues { ["Id"] = id }, cancellationToken);
        if (existing is null) return NotFound(ApiEnvelope.Fail("employee_not_found", $"No employee with id '{id}'."));

        existing.EmployeeCode = employee.EmployeeCode;
        existing.Name = employee.Name;
        existing.Email = employee.Email;
        existing.Department = employee.Department;
        existing.IsActive = employee.IsActive;

        var result = await _crud.UpdateAsync(existing, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(ApiEnvelope.Fail("validation_failed", "Employee failed validation.", ToFieldErrors(result.Errors)));

        return Ok(ApiEnvelope.Ok(existing));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var authz = await _authorization.AuthorizeAsync(EntityKey, "delete", cancellationToken);
        if (!authz.Allowed) return Forbidden(authz.Reason);

        var existing = await _crud.GetAsync(new EntityKeyValues { ["Id"] = id }, cancellationToken);
        if (existing is null) return NotFound(ApiEnvelope.Fail("employee_not_found", $"No employee with id '{id}'."));

        var result = await _crud.DeleteAsync(new EntityKeyValues { ["Id"] = id }, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(ApiEnvelope.Fail("delete_failed", "Delete failed."));

        return Ok(ApiEnvelope.Ok<object?>(null));
    }

    private ObjectResult Forbidden(string? reason) =>
        StatusCode(StatusCodes.Status403Forbidden, ApiEnvelope.Fail("forbidden", reason ?? $"Not authorized for '{EmployeeEntityDefinitionProvider.EntityName}'."));

    private static Dictionary<string, string[]> ToFieldErrors(IReadOnlyList<APIPlatform.Foundation.Results.ErrorInfo> errors) =>
        errors
            .GroupBy(e => e.Field ?? "_")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
}
