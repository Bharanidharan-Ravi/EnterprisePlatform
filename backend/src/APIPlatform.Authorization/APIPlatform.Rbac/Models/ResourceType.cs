namespace APIPlatform.Rbac.Models;

/// <summary>
/// The kinds of resource APIPlatform.Rbac can authorize against. This is the single enum the
/// entire pipeline is data-driven around — Section 9.7 of the Master Plan requires RBAC to
/// stay generic across many generated apps, so this list must never grow app-specific values
/// (e.g. no "LeaveRequest" here — that belongs to a generated app's own PermissionKey strings).
/// </summary>
public enum ResourceType
{
    Api,
    Crud,
    Field,
    Row,
    Menu,
    Feature,
    Policy
}
