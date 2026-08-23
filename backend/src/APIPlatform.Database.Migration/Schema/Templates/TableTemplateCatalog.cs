using APIPlatform.Database.Migration.Schema.Models;

namespace APIPlatform.Database.Migration.Schema.Templates;

/// <summary>
/// The set of predefined tables the engine knows by name — the baseline nearly every app built on
/// this platform needs (a login, its roles and permissions, an audit trail, notifications,
/// settings, attachments), regardless of whether that app is a CRM, an HRMS, or something else.
///
/// <para>Naming one of these in a request creates it without the caller restating its columns;
/// naming anything else creates a new table from the request's own fields. Extra fields are
/// additive in both cases, so a template is a starting point rather than a fixed shape.</para>
///
/// <para>Templates deliberately declare no key or audit columns of their own — the engine appends
/// <c>Id</c> and the audit set to every table it creates, so those stay defined in exactly one
/// place rather than being repeated (and drifting) across nine template definitions.</para>
/// </summary>
public static class TableTemplateCatalog
{
    private static FieldDefinition Field(
        string name, string type = "string", int? maxLength = null, bool nullable = true,
        bool unique = false, bool indexed = false) =>
        new() { Name = name, Type = type, MaxLength = maxLength, Nullable = nullable, Unique = unique, Indexed = indexed };

    private static readonly TableTemplate[] All =
    [
        new()
        {
            Key = "login",
            TableName = "Logins",
            Description = "User login records — credentials, display name, and account state.",
            Fields =
            [
                Field("Username", maxLength: 100, nullable: false, unique: true),
                Field("FirstName", maxLength: 100, nullable: false),
                Field("LastName", maxLength: 100, nullable: false),
                Field("PasswordHash", maxLength: 256, nullable: false),
                Field("PasswordSalt", maxLength: 256, nullable: false),
                Field("Email", maxLength: 256, indexed: true),
                Field("PhoneNumber", maxLength: 32),
                Field("IsActive", "bool", nullable: false),
                Field("IsLocked", "bool", nullable: false),
                Field("FailedAttemptCount", "int", nullable: false),
                Field("LastLoginOnUtc", "datetime")
            ]
        },
        new()
        {
            Key = "role",
            TableName = "Roles",
            Description = "Named roles that logins are granted.",
            Fields =
            [
                Field("Code", maxLength: 64, nullable: false, unique: true),
                Field("Name", maxLength: 128, nullable: false),
                Field("Description", maxLength: 400),
                Field("IsActive", "bool", nullable: false)
            ]
        },
        new()
        {
            Key = "permission",
            TableName = "Permissions",
            Description = "Individual permissions that roles are composed of.",
            Fields =
            [
                Field("Code", maxLength: 128, nullable: false, unique: true),
                Field("Name", maxLength: 128, nullable: false),
                Field("Category", maxLength: 64, indexed: true),
                Field("Description", maxLength: 400)
            ]
        },
        new()
        {
            Key = "userrole",
            TableName = "UserRoles",
            Description = "Assignment of roles to logins.",
            Fields =
            [
                Field("UserId", "guid", nullable: false, indexed: true),
                Field("RoleId", "guid", nullable: false, indexed: true)
            ]
        },
        new()
        {
            Key = "rolepermission",
            TableName = "RolePermissions",
            Description = "Assignment of permissions to roles.",
            Fields =
            [
                Field("RoleId", "guid", nullable: false, indexed: true),
                Field("PermissionId", "guid", nullable: false, indexed: true)
            ]
        },
        new()
        {
            Key = "audit",
            TableName = "AuditLogs",
            Description = "Row-level change history — who changed what, and to what.",
            Fields =
            [
                Field("EntityType", maxLength: 128, nullable: false, indexed: true),
                Field("EntityId", maxLength: 128, nullable: false, indexed: true),
                Field("Action", maxLength: 32, nullable: false),
                Field("UserId", "guid", indexed: true),
                Field("OldValues", "json"),
                Field("NewValues", "json"),
                Field("IpAddress", maxLength: 64),
                Field("OccurredOnUtc", "datetime", nullable: false, indexed: true)
            ]
        },
        new()
        {
            Key = "notification",
            TableName = "Notifications",
            Description = "In-app notifications raised by the application.",
            Fields =
            [
                Field("Application", maxLength: 64, nullable: false, indexed: true),
                Field("EntityType", maxLength: 128),
                Field("EntityId", maxLength: 128),
                Field("EventType", maxLength: 128, nullable: false),
                Field("Title", maxLength: 400, nullable: false),
                Field("Message", "text"),
                Field("Data", "json")
            ]
        },
        new()
        {
            Key = "setting",
            TableName = "Settings",
            Description = "Key/value application configuration held in the database.",
            Fields =
            [
                Field("Category", maxLength: 64, nullable: false, indexed: true),
                Field("Key", maxLength: 128, nullable: false),
                Field("Value", "text"),
                Field("ValueType", maxLength: 32),
                Field("IsEditable", "bool", nullable: false)
            ]
        },
        new()
        {
            Key = "attachment",
            TableName = "Attachments",
            Description = "File metadata attached to any entity; bytes live in the storage module.",
            Fields =
            [
                Field("EntityType", maxLength: 128, nullable: false, indexed: true),
                Field("EntityId", maxLength: 128, nullable: false, indexed: true),
                Field("FileName", maxLength: 400, nullable: false),
                Field("ContentType", maxLength: 128),
                Field("SizeInBytes", "long", nullable: false),
                Field("StoragePath", maxLength: 1000, nullable: false)
            ]
        }
    ];

    private static readonly Dictionary<string, TableTemplate> ByKey =
        All.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every predefined table, for a catalog/listing endpoint.</summary>
    public static IReadOnlyList<TableTemplate> Templates => All;

    /// <summary>Looks up a template by its key (<c>login</c>), case-insensitively. A template's
    /// key is never a table name — <see cref="Models.TableDefinition.Table"/> is a separate,
    /// independent field — so this never matches against <see cref="TableTemplate.TableName"/>.</summary>
    public static bool TryGet(string? key, out TableTemplate template)
    {
        template = null!;
        return !string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key.Trim(), out template!);
    }
}
