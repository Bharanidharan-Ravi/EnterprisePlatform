using System.Linq;
using System.Threading.Tasks;
using APIPlatform.Authentication.Context;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

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
            return Ok(response);
        }
        return Unauthorized(response);
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var current = _currentUserContextAccessor.Current;
        return Ok(new
        {
            UserId = current.UserId,
            Username = current.Username,
            IsAuthenticated = current.IsAuthenticated,
            Claims = current.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, request.UserId);
        if (response.Ok)
        {
            return Ok(response);
        }
        return Unauthorized(response);
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

public class HashRequest
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public required string Password { get; set; }
}
