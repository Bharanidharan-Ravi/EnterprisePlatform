using APIPlatform.Authentication.Claims;
using APIPlatform.Authentication.Context;
using APIPlatform.Authentication.Events;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Jwt;
using APIPlatform.Authentication.Models;
using APIPlatform.Authentication.Pipeline;
using APIPlatform.Authentication.Pipeline.Stages;
using APIPlatform.Authentication.Security;
using APIPlatform.Authentication.Services;
using APIPlatform.Authentication.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace APIPlatform.Authentication.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full APIPlatform.Authentication module.
    ///
    /// Required from the consuming app:
    ///   services.AddScoped&lt;IIdentityResolver, YourUserResolver&gt;();
    ///   services.Configure&lt;JwtOptions&gt;(config.GetSection(JwtOptions.Section));
    ///
    /// Optional overrides (register before calling AddAuthentication to take precedence):
    ///   IPasswordHasher        — replace PBKDF2 with BCrypt/Argon2
    ///   IAuthenticationPlanner — custom strategy resolution
    ///   IAuthenticationExecutor— custom execution (External providers)
    ///   ISessionStore          — DB/Redis-backed sessions
    ///   IRefreshTokenStore     — DB/Redis-backed refresh tokens
    ///   IAuthenticationEventPublisher — Audit/Notification integration
    ///   IClaimsBuilderExtension— app-specific JWT claims
    /// </summary>
    public static IServiceCollection AddAuthenticationPlatform(this IServiceCollection services)
    {
        // Security
        services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.TryAddScoped<IPasswordService, PasswordService>();

        // JWT
        services.TryAddScoped<IJwtService, JwtService>();

        // Session
        services.TryAddSingleton<ISessionStore, InMemorySessionStore>();
        services.TryAddScoped<ISessionService, SessionService>();

        // Refresh tokens
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.TryAddScoped<IRefreshTokenService, RefreshTokenService>();

        // Claims
        services.TryAddScoped<IClaimsBuilder, ClaimsBuilder>();

        // Events (no-op default; Audit module replaces this)
        services.TryAddSingleton<IAuthenticationEventPublisher, NoOpAuthenticationEventPublisher>();

        // Core services
        services.TryAddScoped<IAuthenticationPlanner, AuthenticationPlanner>();
        services.TryAddScoped<IAuthenticationExecutor, AuthenticationExecutor>();

        // Pipeline stages
        services.AddScoped<IdentityResolutionStage>();
        services.AddScoped<ContextEnrichmentStage>();
        services.AddScoped<ValidationStage>();
        services.AddScoped<AuthenticationPlanningStage>();
        services.AddScoped<AuthenticationExecutionStage>();
        services.AddScoped<ResponseMappingStage>();

        // Pipeline + public API
        services.TryAddScoped<IAuthenticationPipeline, AuthenticationPipeline>();
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();

        // CurrentUserContext — platform-wide identity abstraction (host-independent)
        services.AddScoped<ICurrentUserContextAccessor, CurrentUserContextAccessor>();

        // Settings default (apps override via Configure<AuthenticationSettings>)
        services.TryAddSingleton(Microsoft.Extensions.Options.Options.Create(new AuthenticationSettings()));

        return services;
    }

    /// <summary>Register an app-specific claims extension. Multiple extensions may be
    /// registered; all run in registration order.</summary>
    public static IServiceCollection AddClaimsExtension<T>(this IServiceCollection services)
        where T : class, IClaimsBuilderExtension
    {
        services.AddScoped<IClaimsBuilderExtension, T>();
        return services;
    }

    /// <summary>Register a future external auth provider (OAuth, LDAP, Azure AD etc.).</summary>
    public static IServiceCollection AddExternalAuthProvider<T>(this IServiceCollection services)
        where T : class, IExternalAuthProvider
    {
        services.AddScoped<IExternalAuthProvider, T>();
        return services;
    }
}

/// <summary>ASP.NET Core pipeline extensions — kept separate so non-web hosts can still use
/// AddAuthenticationPlatform() without needing IApplicationBuilder.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the CurrentUserContextMiddleware. Must be placed after app.UseAuthentication()
    /// so ClaimsPrincipal is already populated when this middleware runs.
    ///
    ///   app.UseAuthentication();
    ///   app.UseCurrentUserContext();   ← here
    ///   app.UseAuthorization();
    /// </summary>
    public static IApplicationBuilder UseCurrentUserContext(this IApplicationBuilder app)
        => app.UseMiddleware<CurrentUserContextMiddleware>();
}
