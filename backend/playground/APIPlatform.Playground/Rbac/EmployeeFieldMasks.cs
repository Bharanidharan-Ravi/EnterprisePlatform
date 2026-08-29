namespace APIPlatform.Playground.Rbac;

/// <summary>
/// Field-mask constants for Employee — the naming half of the seeded
/// <see cref="APIPlatform.Rbac.Models.FieldPermissionRule"/> below, kept in one place so
/// <see cref="Services.EmployeeModuleInitializationService"/>'s seeding doesn't repeat magic
/// strings. Unlike <see cref="EmployeeRowFilters"/>, field masking needs no named-delegate
/// registration — <see cref="APIPlatform.Rbac.Contexts.FieldMaskDescriptor.FromRules"/> is pure
/// data (rule + PermissionSet), so there's no registry to populate at startup.
/// </summary>
public static class EmployeeFieldMasks
{
    /// <summary>Must match <see cref="Models.Employee"/>.Email's property name — FieldMaskCrudHook
    /// resolves it by reflection, case-insensitively.</summary>
    public const string EmailField = "Email";

    /// <summary>Holding this grants Write access to Email (see the seeded rule); not holding it
    /// means None — masking hides the field entirely rather than degrading to read-only, since
    /// nothing about Email being visible-but-uneditable makes sense for this field.</summary>
    public const string EmailAccessPermissionKey = "employee.email.read";
}
