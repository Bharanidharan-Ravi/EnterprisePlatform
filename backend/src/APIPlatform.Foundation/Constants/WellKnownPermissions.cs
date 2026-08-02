namespace APIPlatform.Foundation.Constants;

/// <summary>Generic, cross-cutting permission names. Domain-specific permissions belong to the consuming application, never here.</summary>
public static class WellKnownPermissions
{
    public const string Read = "generic:read";
    public const string Write = "generic:write";
    public const string Delete = "generic:delete";
    public const string Admin = "generic:admin";
}
