using APIPlatform.Authentication.DependencyInjection;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Jwt;
using APIPlatform.Playground.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APIPlatform.Playground.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public static IServiceCollection AddAPIPlatformAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Real user store: [Nucleus].[dbo].[Logins], via IDynamicQueryService — not the two
        // hardcoded logins PlaygroundIdentityResolver used for the Phase 2 RBAC proof. NOTE: RBAC
        // role grants in EmployeeModuleInitializationService are still seeded against the hardcoded
        // "user-123"/"user-456" ids from that class, so real Logins users will authenticate fine
        // but land with no role assigned (Employee CRUD denies by default) until that seeding is
        // updated to target real Ids too.
        services.AddScoped<IIdentityResolver, LoginsIdentityResolver>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

        services.AddAuthenticationPlatform();
        
        var jwtOptions = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>();
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (jwtOptions != null)
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            }

            // Without this, a rejected token just shows up as an opaque "invalid_token" — no way
            // to tell expired/wrong-signature/wrong-issuer apart from the response or the logs, which
            // is exactly the trap of a request that comes back 200 with IsAuthenticated: false and no
            // explanation. OnAuthenticationFailed logs the real exception server-side; OnChallenge
            // surfaces its message in the 401's WWW-Authenticate header for [Authorize] endpoints.
            options.Events = new JwtBearerEvents
            {
                // A JWT is self-contained: once signed, it validates purely on its own signature +
                // expiry, with nothing server-side to check against — so refreshing does NOT, by
                // itself, invalidate an old access token still inside its expiry window. This closes
                // that gap: every access token carries a "sid" claim tied to a session record
                // (ClaimsBuilder), and AuthenticationService.RefreshAsync revokes the OLD session on
                // every refresh. Checking that session here, on every authenticated request, is what
                // actually makes an old token stop working the moment its session is revoked, instead
                // of staying valid until its own expiry regardless of refresh.
                OnTokenValidated = async context =>
                {
                    var sessionId = context.Principal?.FindFirst("sid")?.Value;
                    if (string.IsNullOrEmpty(sessionId))
                        return; // no session claim on this token (shouldn't happen post-login/refresh) — allow through unchanged

                    var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                    if (!await sessions.ValidateAsync(sessionId, context.HttpContext.RequestAborted))
                    {
                        context.Fail("Session has been revoked (superseded by a refresh, or logged out).");
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(context.Exception, "JWT validation failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // HTTP header values cannot contain control characters (CR/LF especially —
                    // Kestrel throws rather than send them, per RFC 7230). Exception messages are
                    // free text and do contain them (e.g. SecurityTokenExpiredException's message
                    // spans multiple lines), so every control character is flattened to a space
                    // before this goes anywhere near a header — never pass .Message through raw.
                    if (context.AuthenticateFailure is not null)
                        context.ErrorDescription = SanitizeForHeader(context.AuthenticateFailure.Message);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static string SanitizeForHeader(string value) =>
        new(value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
}
