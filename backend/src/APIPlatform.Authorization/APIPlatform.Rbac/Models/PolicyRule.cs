namespace APIPlatform.Rbac.Models;

/// <summary>
/// A policy rule attached to a permission key. Name is a lookup key into IPolicyRuleRegistry —
/// the actual boolean logic is a named delegate registered by the CONSUMING app, never inside
/// Rbac itself (mirrors the Workflow engine's named-condition extension pattern, Section 7.2).
/// </summary>
public sealed class PolicyRule
{
    public required string Name { get; init; }
    public required string PermissionKey { get; init; }
    public required ResourceType ResourceType { get; init; }
    public int Priority { get; init; }
}
