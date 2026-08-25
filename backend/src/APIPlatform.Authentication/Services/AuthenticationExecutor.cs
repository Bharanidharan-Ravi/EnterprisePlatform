using APIPlatform.Authentication.Events;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Services;

/// <summary>Default IAuthenticationExecutor — Local strategy. Verifies password, generates
/// claims + JWT, optionally creates refresh token + session, publishes event. External strategy
/// providers (OAuth, LDAP) are wired via IExternalAuthProvider registry in a future step.</summary>
public sealed class AuthenticationExecutor : IAuthenticationExecutor
{
    private readonly IPasswordService           _passwords;
    private readonly IClaimsBuilder             _claims;
    private readonly IJwtService                _jwt;
    private readonly ISessionService            _sessions;
    private readonly IRefreshTokenService       _refresh;
    private readonly IAuthenticationEventPublisher _events;

    public AuthenticationExecutor(
        IPasswordService            passwords,
        IClaimsBuilder              claims,
        IJwtService                 jwt,
        ISessionService             sessions,
        IRefreshTokenService        refresh,
        IAuthenticationEventPublisher events)
    {
        _passwords = passwords;
        _claims    = claims;
        _jwt       = jwt;
        _sessions  = sessions;
        _refresh   = refresh;
        _events    = events;
    }

    public async Task ExecuteAsync(AuthenticationContext context)
    {
        // 1. Verify password
        context.PasswordVerified = _passwords.Verify(context.Request.Password, context.User!.PasswordHash);
        if (!context.PasswordVerified)
        {
            context.ShortCircuited = true;
            context.ErrorCode    = "INVALID_CREDENTIALS";
            context.ErrorMessage = "Invalid credentials.";
            await _events.PublishAsync(FailedEvent(context), context.CancellationToken);
            return;
        }

        // 2. Session — created BEFORE claims/JWT (below) so its id is available to embed as the
        // "sid" claim. Building claims first (the original order here) left context.SessionId null
        // at claims-build time, so login-issued tokens never carried "sid" — which silently disabled
        // Playground's OnTokenValidated session-revocation check for every access token minted at
        // login: logout/refresh would revoke the session server-side, but the still-unexpired old
        // access token kept authenticating anyway, because there was no "sid" claim on it to check.
        // (The refresh path already set SessionId before building claims, so only login was affected.)
        var session = await _sessions.CreateAsync(context, context.CancellationToken);
        context.SessionId = session.SessionId;

        // 3. Build claims + JWT
        context.GeneratedClaims   = _claims.Build(context);
        (context.AccessToken, var expiry) = _jwt.Generate(context.GeneratedClaims);
        context.AccessTokenExpiry = expiry;

        // 4. Refresh token (optional per plan)
        if (context.Plan?.GenerateRefreshToken == true)
        {
            (context.RefreshToken, context.RefreshTokenExpiry) = _refresh.Generate(context.User.UserId, context.SessionId);
        }

        // 5. Publish success event
        await _events.PublishAsync(new AuthenticationEvent
        {
            EventType  = AuthenticationEventType.LoginSuccess,
            OccurredAt = context.CurrentTime,
            UserId     = context.User.UserId,
            Username   = context.User.Username,
            TenantId   = context.User.TenantId,
            ClientIp   = context.Device?.ClientIp,
            DeviceId   = context.Device?.DeviceId
        }, context.CancellationToken);
    }

    private static AuthenticationEvent FailedEvent(AuthenticationContext ctx) => new()
    {
        EventType  = AuthenticationEventType.LoginFailed,
        OccurredAt = ctx.CurrentTime,
        Username   = ctx.Request.LoginIdentifier,
        ClientIp   = ctx.Device?.ClientIp,
        DeviceId   = ctx.Device?.DeviceId,
        ErrorCode  = ctx.ErrorCode
    };
}
