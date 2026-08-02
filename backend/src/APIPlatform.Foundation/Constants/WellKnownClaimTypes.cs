namespace APIPlatform.Foundation.Constants;

/// <summary>Common claim type keys used with <see cref="Interfaces.ICurrentUser"/>.Claims across the platform.</summary>
public static class WellKnownClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "preferred_username";
    public const string Email = "email";
    public const string Department = "department";
    public const string Role = "role";
    public const string Language = "language";
    public const string Timezone = "timezone";
    public const string TenantId = "tenant_id";
}
