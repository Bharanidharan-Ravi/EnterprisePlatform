namespace APIPlatform.Rbac.Attributes;

/// <summary>
/// Marker attribute (Master Plan Section 3.4). Declares required permission only — it does
/// NOT enforce anything by itself. Enforcement is a Host/Middleware concern that reads this
/// attribute and calls IPermissionEvaluator; Rbac deliberately has no ASP.NET Core dependency,
/// preserving minimal-dependency/provider-independence (Hard Rule 3).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string PermissionKey { get; }
    public string Action { get; }

    public RequirePermissionAttribute(string permissionKey, string action = "Execute")
    {
        PermissionKey = permissionKey;
        Action = action;
    }
}
