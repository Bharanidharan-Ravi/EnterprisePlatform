using System.Linq;
using System.Threading.Tasks;
using APIPlatform.Authentication.Context;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
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

    public AuthenticationController(
        IAuthenticationService authService,
        IPasswordHasher passwordHasher,
        ICurrentUserContextAccessor currentUserContextAccessor)
    {
        _authService = authService;
        _passwordHasher = passwordHasher;
        _currentUserContextAccessor = currentUserContextAccessor;
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
    /// KNOWN LIMITATION (documented, not fixed this phase — see phase2 report Section N):
    /// AuthenticationService.RefreshAsync always revokes the refresh token and returns
    /// Ok=false/REAUTH_REQUIRED, even for a valid token, by explicit platform design ("lightweight
    /// rotation only, full re-auth pipeline intentionally not re-run here"). This endpoint's
    /// contract is still fixed to match the frontend (envelope + the request shape the backend
    /// actually requires), but a genuine refresh will not succeed until that platform behavior
    /// changes — out of scope for phase2.md 24 ("do not turn this into the full authentication
    /// modernization phase").
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

    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        return Ok(new { Message = "You are authenticated" });
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
