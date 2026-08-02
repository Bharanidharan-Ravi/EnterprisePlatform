using APIPlatform.Authentication.Models;

namespace APIPlatform.Authentication.Pipeline.Stages;

/// <summary>Stage 6 — builds the public AuthenticationResponse. Only response construction;
/// never touches execution, validation, or planning.</summary>
public sealed class ResponseMappingStage : IAuthenticationStage
{
    public Task ExecuteAsync(AuthenticationContext context)
    {
        if (context.ShortCircuited)
        {
            context.Response = new AuthenticationResponse
            {
                Ok = false,
                ErrorCode = context.ErrorCode,
                ErrorMessage = context.ErrorMessage
            };
            return Task.CompletedTask;
        }

        context.Response = new AuthenticationResponse
        {
            Ok = true,
            AccessToken  = context.AccessToken,
            RefreshToken = context.RefreshToken,
            ExpiresAt    = context.AccessTokenExpiry,
            SessionId    = context.SessionId,
            User = context.User is null ? null : new UserProfile
            {
                UserId   = context.User.UserId,
                Username = context.User.Username,
                Email    = context.User.Email,
                RoleIds  = context.User.RoleIds
            }
        };
        return Task.CompletedTask;
    }
}
