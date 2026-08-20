using APIPlatform.Rbac.Models;

namespace APIPlatform.Rbac.Contexts;

/// <summary>Per-field access result for Field-Level Authorization.</summary>
public sealed class FieldMaskDescriptor
{
    public IReadOnlyDictionary<string, FieldAccess> FieldAccess { get; init; } =
        new Dictionary<string, FieldAccess>();

    public static FieldMaskDescriptor Empty { get; } = new();

    public static FieldMaskDescriptor FromRules(
        IReadOnlyCollection<FieldPermissionRule> rules,
        PermissionSet permissions)
    {
        var dict = new Dictionary<string, FieldAccess>();
        foreach (var rule in rules)
        {
            var grantedByPermission =
                permissions.AllowedKeys.Contains(rule.PermissionKey) &&
                !permissions.DeniedKeys.Contains(rule.PermissionKey);

            dict[rule.FieldKey] = grantedByPermission ? rule.Access : Models.FieldAccess.None;
        }
        return new FieldMaskDescriptor { FieldAccess = dict };
    }
}
