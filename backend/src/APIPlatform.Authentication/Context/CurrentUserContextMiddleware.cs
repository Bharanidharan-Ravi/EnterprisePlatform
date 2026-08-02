using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace APIPlatform.Authentication.Context;

/// <summary>
/// ASP.NET Core middleware — the ONLY place in the platform that reads ClaimsPrincipal from
/// HttpContext. Runs after JWT bearer middleware; builds an ICurrentUserContext and places it
/// into the scoped ICurrentUserContextAccessor so all downstream services are host-independent.
///
/// Register with: app.UseCurrentUserContext()  (see ApplicationBuilderExtensions).
/// Must be placed after app.UseAuthentication() in the middleware pipeline.
/// </summary>
public sealed class CurrentUserContextMiddleware
{
    private readonly RequestDelegate _next;
    public CurrentUserContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext, ICurrentUserContextAccessor accessor)
    {
        if (httpContext.User?.Identity?.IsAuthenticated == true)
            accessor.Set(CurrentUserContext.FromClaims(httpContext.User.Claims.ToList()));
        else
            accessor.Clear();

        await _next(httpContext);
    }
}
