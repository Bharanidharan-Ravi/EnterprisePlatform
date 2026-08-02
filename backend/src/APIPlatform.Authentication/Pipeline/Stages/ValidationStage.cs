using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 3 — determines whether execution may continue. Validates request shape,
/// user existence, account state, lockout, password expiry. No SQL, no JWT, no session.</summary>
public sealed class ValidationStage : IAuthenticationStage
{
    public Task ExecuteAsync(AuthenticationContext context)
    {
        // Request validation
        if (string.IsNullOrWhiteSpace(context.Request.LoginIdentifier))
            return Fail(context, "IDENTIFIER_REQUIRED", "Login identifier is required.");
        if (string.IsNullOrWhiteSpace(context.Request.Password))
            return Fail(context, "PASSWORD_REQUIRED", "Password is required.");

        // Identity validation
        if (context.User is null)
            return Fail(context, "USER_NOT_FOUND", "Invalid credentials.");
        if (!context.User.IsActive)
            return Fail(context, "USER_INACTIVE", "Account is inactive.");

        // Lockout validation
        if (context.User.IsLocked)
        {
            if (context.User.LockedUntil.HasValue && context.User.LockedUntil > context.CurrentTime)
                return Fail(context, "ACCOUNT_LOCKED", "Account is temporarily locked.");
        }

        // Password expiry
        if (context.Settings?.PasswordExpiryEnabled == true &&
            context.User.PasswordExpiresAt.HasValue &&
            context.User.PasswordExpiresAt < context.CurrentTime)
            return Fail(context, "PASSWORD_EXPIRED", "Password has expired.");

        return Task.CompletedTask;
    }

    private static Task Fail(AuthenticationContext ctx, string code, string message)
    {
        ctx.ShortCircuited = true;
        ctx.ErrorCode = code;
        ctx.ErrorMessage = message;
        return Task.CompletedTask;
    }
}
