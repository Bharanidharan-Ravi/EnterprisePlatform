using APIPlatform.Rbac.Contexts;
using APIPlatform.Rbac.Models;
using Xunit;

namespace APIPlatform.Rbac.Tests.Contexts;

/// <summary>
/// Direct unit tests on FieldMaskDescriptor.FromRules — the method that failed to compile with
/// CS0120 before Phase 1 (the FieldAccess property and the FieldAccess enum share a name; the fix
/// fully-qualifies the enum reference as Models.FieldAccess.None). These confirm the fix is not
/// just a compile-time patch but produces the correct runtime mask.
/// </summary>
public class FieldMaskDescriptorTests
{
    [Fact]
    public void FromRules_PermissionGranted_ReturnsRuleAccess()
    {
        var permissions = new PermissionSet
        {
            TenantId = "tenant-1",
            UserId = "user-1",
            AllowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Widget.Read" }
        };
        var rules = new[]
        {
            new FieldPermissionRule { EntityKey = "Widget", FieldKey = "Price", PermissionKey = "Widget.Read", Access = FieldAccess.Write }
        };

        var mask = FieldMaskDescriptor.FromRules(rules, permissions);

        Assert.Equal(FieldAccess.Write, mask.FieldAccess["Price"]);
    }

    [Fact]
    public void FromRules_PermissionNotGranted_ReturnsNone()
    {
        var permissions = new PermissionSet { TenantId = "tenant-1", UserId = "user-1" };
        var rules = new[]
        {
            new FieldPermissionRule { EntityKey = "Widget", FieldKey = "Price", PermissionKey = "Widget.Read", Access = FieldAccess.Write }
        };

        var mask = FieldMaskDescriptor.FromRules(rules, permissions);

        Assert.Equal(FieldAccess.None, mask.FieldAccess["Price"]);
    }

    [Fact]
    public void FromRules_PermissionDenied_ReturnsNoneEvenIfAlsoAllowed()
    {
        // Deny overrides allow at the grant level (PermissionResolver), but FieldMaskDescriptor
        // re-checks both sets defensively — a key present in both would be a resolver bug, not a
        // state FromRules should ever trust blindly.
        var permissions = new PermissionSet
        {
            TenantId = "tenant-1",
            UserId = "user-1",
            AllowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Widget.Read" },
            DeniedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Widget.Read" }
        };
        var rules = new[]
        {
            new FieldPermissionRule { EntityKey = "Widget", FieldKey = "Price", PermissionKey = "Widget.Read", Access = FieldAccess.Write }
        };

        var mask = FieldMaskDescriptor.FromRules(rules, permissions);

        Assert.Equal(FieldAccess.None, mask.FieldAccess["Price"]);
    }

    [Fact]
    public void Empty_HasNoFieldEntries()
    {
        Assert.Empty(FieldMaskDescriptor.Empty.FieldAccess);
    }
}
