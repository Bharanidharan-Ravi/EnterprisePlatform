using System.Linq;
using System.Threading.Tasks;
using APIPlatform.Authentication.Context;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Exceptions;
using APIPlatform.Playground.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

/// <summary>
/// Phase 2: Login/Refresh responses are wrapped in <see cref="ApiEnvelope{T}"/> so
/// ui-platform-foundation's apiRequest()/unwrapResponse() (which requires {success,data,error})
/// can consume them — previously the raw AuthenticationResponse was returned directly, which
/// unwrapResponse would always treat as a failure (no top-level "success" field). A logout
/// action was also added; none existed before, but ui-platform-auth's AuthService always calls
/// one on logout.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
    private readonly IDynamicCommandService _dynamicCommand;
    private readonly IDynamicQueryService _dynamicQuery;

    public AuthenticationController(
        IAuthenticationService authService,
        IPasswordHasher passwordHasher,
        ICurrentUserContextAccessor currentUserContextAccessor,
        IDynamicCommandService dynamicCommand,
        IDynamicQueryService dynamicQuery)
    {
        _authService = authService;
        _passwordHasher = passwordHasher;
        _currentUserContextAccessor = currentUserContextAccessor;
        _dynamicCommand = dynamicCommand;
        _dynamicQuery = dynamicQuery;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
    {
        var response = await _authService.AuthenticateAsync(request);
        if (response.Ok)
        {
            return Ok(ApiEnvelope.Ok(response));
        }
        return Unauthorized(ApiEnvelope.Fail(response.ErrorCode ?? "authentication_failed", response.ErrorMessage ?? "Authentication failed."));
    }

    /// <summary>
    /// Generic registration: hashes <see cref="DynamicRegistrationRequest.PlainPassword"/> via the
    /// auth engine's IPasswordHasher, drops the result into
    /// <see cref="DynamicRegistrationRequest.PasswordColumn"/> alongside the rest of Values, and
    /// inserts the row through IDynamicCommandService. Table shape and column names are entirely
    /// caller-supplied — this is the "reload user data" write counterpart, so a generated app never
    /// gets a hand-written SQL_USER-shaped registration endpoint baked into the platform.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] DynamicRegistrationRequest request, CancellationToken cancellationToken)
    {
        var passwordHash = _passwordHasher.Hash(request.PlainPassword);

        var values = new Dictionary<string, object?>(request.Values, StringComparer.OrdinalIgnoreCase)
        {
            [request.PasswordColumn] = passwordHash
        };
        ApplyAuditStamps(values, request.CreatedByColumn, request.CreatedBy, request.CreatedOnColumn);

        try
        {
            var rowsInserted = await _dynamicCommand.InsertAsync(
                new DynamicInsertRequest { TableName = request.TableName, Values = values },
                cancellationToken);

            return Ok(ApiEnvelope.Ok(new { Inserted = rowsInserted }));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiEnvelope.Fail(
                "validation_failed",
                "The registration request failed validation.",
                ex.Errors.ToDictionary(e => e.Key, e => e.Value)));
        }
    }

    /// <summary>
    /// Bulk import: reads <see cref="DynamicMigrationRequest.SourceTable"/> through
    /// IDynamicQueryService (SourceTable may be database/schema-qualified — e.g.
    /// "IQS_DB.dbo.SEC_USER" read across into whatever database this app's own connection string
    /// points at), remaps each row's columns onto TargetTable's shape via ColumnMap, stamps every
    /// migrated row with one shared hash of PlainPassword (a temporary password every imported
    /// account gets, same idea as Register but for N rows from an existing source table instead of
    /// one caller-supplied row), merges in FixedValues (target columns with no source counterpart —
    /// audit columns, IsActive defaults, etc.), and writes each row through IDynamicCommandService.
    /// Both table shapes and the mapping between them are entirely caller-supplied; nothing here
    /// names SEC_USER, Logins, or any other concrete table. One bad row does not abort the batch —
    /// failures are collected and returned per row alongside the rows that succeeded.
    /// </summary>
    [HttpPost("import")]
    [AllowAnonymous]
    public async Task<IActionResult> Import([FromBody] DynamicMigrationRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sourceRows;
        try
        {
            sourceRows = await _dynamicQuery.QueryAsync(new DynamicQueryRequest
            {
                TableName = request.SourceTable,
                Columns = request.SourceColumns,
                Filters = request.SourceFilters,
                Top = request.SourceTop
            }, cancellationToken);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiEnvelope.Fail(
                "validation_failed",
                "The source query failed validation.",
                ex.Errors.ToDictionary(e => e.Key, e => e.Value)));
        }

        var passwordHash = _passwordHasher.Hash(request.PlainPassword);
        var inserted = 0;
        var failures = new List<object>();

        for (var i = 0; i < sourceRows.Count; i++)
        {
            var values = new Dictionary<string, object?>(request.FixedValues, StringComparer.OrdinalIgnoreCase);
            foreach (var (sourceColumn, targetColumn) in request.ColumnMap)
                values[targetColumn] = sourceRows[i].TryGetValue(sourceColumn, out var value) ? value : null;
            values[request.PasswordColumn] = passwordHash;
            ApplyAuditStamps(values, request.CreatedByColumn, request.CreatedBy, request.CreatedOnColumn);

            try
            {
                await _dynamicCommand.InsertAsync(new DynamicInsertRequest { TableName = request.TargetTable, Values = values }, cancellationToken);
                inserted++;
            }
            catch (ValidationException ex)
            {
                failures.Add(new { Row = i, Errors = ex.Errors });
            }
        }

        return Ok(ApiEnvelope.Ok(new
        {
            SourceRowCount = sourceRows.Count,
            Inserted = inserted,
            Failed = failures.Count,
            Errors = failures
        }));
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var current = _currentUserContextAccessor.Current;
        return Ok(ApiEnvelope.Ok(new
        {
            UserId = current.UserId,
            Username = current.Username,
            IsAuthenticated = current.IsAuthenticated,
            Claims = current.Claims.Select(c => new { c.Type, c.Value })
        }));
    }

    /// <summary>
    /// On a valid, unexpired refresh token: revokes it (single-use), re-resolves the account via
    /// IIdentityResolver.ResolveByIdAsync (so a since-deactivated/locked account is caught even
    /// though no password check runs here), rebuilds claims, and issues a new access token plus a
    /// rotated refresh token. Password verification is intentionally not re-run — refresh trusts
    /// the token, not a password — matching standard silent-refresh behavior.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, request.UserId);
        if (response.Ok)
        {
            return Ok(ApiEnvelope.Ok(response));
        }
        return Unauthorized(ApiEnvelope.Fail(response.ErrorCode ?? "refresh_failed", response.ErrorMessage ?? "Refresh failed."));
    }

    /// <summary>No logout endpoint existed before Phase 2 — ui-platform-auth's AuthService always
    /// POSTs here on logout. Revokes the session if one is supplied; always succeeds from the
    /// client's point of view (logout is a client-side state clear regardless of server outcome).</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            await _authService.RevokeAsync(request.SessionId);
        }
        return Ok(ApiEnvelope.Ok<object?>(null));
    }

    [HttpPost("hash")]
    public IActionResult Hash([FromBody] HashRequest request)
    {
        var hash = _passwordHasher.Hash(request.Password);
        var verify = _passwordHasher.Verify(request.Password, hash);

        return Ok(new
        {
            HashCreated = hash,
            VerificationResult = verify
        });
    }

    /// <summary>
    /// Wrapped in <see cref="ApiEnvelope{T}"/> like every other endpoint here (see class summary) —
    /// previously returned a raw <c>{ Message }</c> object, which ui-platform-foundation's
    /// unwrapResponse() always treats as a failure (no top-level "success" field), so a UI caller
    /// would see an error even on a 200 with a still-valid token.
    /// </summary>
    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        return Ok(ApiEnvelope.Ok(new { Message = "You are authenticated" }));
    }

    /// <summary>
    /// Stamps CreatedBy/CreatedOn onto a row about to be written, if the caller named target
    /// columns for them. "Who" always comes from the request (only the caller knows that); "when"
    /// is always computed here, server-side, on the theory that a client-supplied timestamp isn't
    /// trustworthy — never taken from Values/FixedValues even if a caller tried to set one there.
    /// Both stamps are opt-in (null column name = table has no such column) so this works whether
    /// or not the target table carries audit columns at all.
    /// </summary>
    private static void ApplyAuditStamps(IDictionary<string, object?> values, string? createdByColumn, string? createdBy, string? createdOnColumn)
    {
        if (!string.IsNullOrWhiteSpace(createdByColumn))
            values[createdByColumn] = createdBy;

        if (!string.IsNullOrWhiteSpace(createdOnColumn))
            values[createdOnColumn] = DateTime.UtcNow;
    }
}

public class RefreshRequest
{
    public required string RefreshToken { get; set; }
    public required string UserId { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
    public string? SessionId { get; set; }
}

public class HashRequest
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public required string Password { get; set; }
}

/// <summary>
/// Table/columns are caller-supplied, same as DynamicQueryRequest/DynamicInsertRequest — the
/// engine (and this controller) never hardcodes a user table shape. PlainPassword never reaches
/// IDynamicCommandService as-is; AuthenticationController.Register hashes it first and writes the
/// result into PasswordColumn.
/// </summary>
public class DynamicRegistrationRequest
{
    public required string TableName { get; set; }
    public required IReadOnlyDictionary<string, object?> Values { get; set; }
    public required string PasswordColumn { get; set; }
    public required string PlainPassword { get; set; }

    /// <summary>Target column to stamp with <see cref="CreatedBy"/>. Omit if the table has no
    /// such column.</summary>
    public string? CreatedByColumn { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>Target column to stamp with the server's current UTC time. Omit if the table has
    /// no such column — the value is always computed here, never taken from the caller.</summary>
    public string? CreatedOnColumn { get; set; }
}

/// <summary>
/// Describes a bulk copy from one table shape into another. SourceTable may be database/schema-
/// qualified for a cross-database read; TargetTable is resolved against this app's own connection
/// (so normally unqualified). ColumnMap keys are source column names (as returned by the source
/// query), values are the target column each one is written into.
/// </summary>
public class DynamicMigrationRequest
{
    public required string SourceTable { get; set; }
    public required IReadOnlyList<string> SourceColumns { get; set; }
    public IReadOnlyDictionary<string, object?> SourceFilters { get; set; } = new Dictionary<string, object?>();
    public int SourceTop { get; set; } = 500;

    public required string TargetTable { get; set; }
    public required IReadOnlyDictionary<string, string> ColumnMap { get; set; }

    /// <summary>Target column/value pairs applied identically to every migrated row — for columns
    /// with no source counterpart (audit stamps, IsActive defaults, etc.).</summary>
    public IReadOnlyDictionary<string, object?> FixedValues { get; set; } = new Dictionary<string, object?>();

    public required string PasswordColumn { get; set; }
    public required string PlainPassword { get; set; }

    /// <summary>Target column to stamp with <see cref="CreatedBy"/> on every migrated row. Omit if
    /// the target table has no such column.</summary>
    public string? CreatedByColumn { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>Target column to stamp with the server's current UTC time on every migrated row.
    /// Omit if the target table has no such column — the value is always computed here, never
    /// taken from SourceFilters/FixedValues/the source row.</summary>
    public string? CreatedOnColumn { get; set; }
}
