using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Templates;
using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Schema.Sql;

/// <summary>
/// Turns a caller's <see cref="TableDefinition"/> into a <see cref="ResolvedTable"/>: resolves
/// <see cref="TableDefinition.Template"/> (if given), works out the physical table name from
/// <see cref="TableDefinition.Table"/> independently of that, appends the caller's extra fields,
/// adds the key and audit columns every table gets, then validates the whole set. This is the
/// only place a template table and a brand-new table differ — after this, both are just a
/// validated column list, which is why create and update need no branching of their own.
///
/// <para>Nothing here touches the database; it is pure input → columns, so its rules can be
/// asserted without a live SQL Server or HANA instance.</para>
/// </summary>
internal static class TableDefinitionResolver
{
    /// <summary>Key column added to every table, unless a caller field claims the primary key.</summary>
    public const string KeyColumn = "Id";

    /// <summary>JSON column carrying per-record values that don't warrant their own column —
    /// how a caller stores extra data without changing the schema at all.</summary>
    public const string AdditionalDataColumn = "AdditionalData";

    /// <summary>Audit columns appended to every table. Named to match the "…OnUtc" convention the
    /// platform's existing tables already use (Notifications.CreatedOnUtc,
    /// MigrationHistory.AppliedOnUtc) rather than introducing a second spelling.</summary>
    private static readonly FieldDefinition[] AuditFields =
    [
        new() { Name = "CreatedBy", Type = "guid", Nullable = true },
        new() { Name = "CreatedOnUtc", Type = "datetime", Nullable = false },
        new() { Name = "LastModifiedBy", Type = "guid", Nullable = true },
        new() { Name = "LastModifiedOnUtc", Type = "datetime", Nullable = true }
    ];

    public static bool TryResolve(
        TableDefinition definition,
        IMigrationSqlDialect dialect,
        out ResolvedTable resolved,
        out string error)
    {
        resolved = null!;
        error = string.Empty;

        var templateKey = definition.Template?.Trim();
        var hasTemplate = !string.IsNullOrWhiteSpace(templateKey);
        TableTemplate template = null!;

        if (hasTemplate && !TableTemplateCatalog.TryGet(templateKey, out template))
        {
            error = $"Unknown template '{templateKey}'. Predefined templates: " +
                    $"{string.Join(", ", TableTemplateCatalog.Templates.Select(t => t.Key))}.";
            return false;
        }

        // Table is independent of Template: an empty Table falls back to the template's own name
        // (so 'login' alone still creates 'Logins'), but a caller who supplies Table always gets
        // that name instead — e.g. the same 'login' template columns under a second table name.
        var requestedTable = definition.Table?.Trim();
        var tableName = !string.IsNullOrWhiteSpace(requestedTable)
            ? requestedTable
            : hasTemplate ? template.TableName : string.Empty;

        if (!SchemaIdentifier.TryValidate(tableName, "Table", out error)) return false;

        var fields = new List<FieldDefinition>();
        if (hasTemplate) fields.AddRange(template.Fields);

        var callerFields = definition.Fields ?? [];
        foreach (var field in callerFields)
        {
            if (!SchemaIdentifier.TryValidate(field.Name, "Field", out error)) return false;

            if (hasTemplate && template.Fields.Any(f => NameEquals(f.Name, field.Name)))
            {
                error = $"Field '{field.Name}' is already part of the '{template.Key}' template — " +
                        "extra fields must be new columns.";
                return false;
            }

            fields.Add(field);
        }

        if (fields.Count == 0)
        {
            error = "No template was given and no fields were supplied, so there are no columns to " +
                    $"create. Set 'template' to one of: {string.Join(", ", TableTemplateCatalog.Templates.Select(t => t.Key))}, " +
                    "or supply 'fields' for a new table.";
            return false;
        }

        var primaryKeys = fields.Where(f => f.PrimaryKey).ToList();
        if (primaryKeys.Count > 1)
        {
            error = $"Only one field may be the primary key; got {primaryKeys.Count} " +
                    $"({string.Join(", ", primaryKeys.Select(f => f.Name))}).";
            return false;
        }

        // The engine's own Id goes first and is the key unless the caller declared their own.
        if (primaryKeys.Count == 0)
            fields.Insert(0, new FieldDefinition { Name = KeyColumn, Type = "guid", Nullable = false, PrimaryKey = true });

        if (definition.IncludeAdditionalData && !fields.Any(f => NameEquals(f.Name, AdditionalDataColumn)))
            fields.Add(new FieldDefinition { Name = AdditionalDataColumn, Type = "json", Nullable = true });

        if (definition.IncludeAudit)
            fields.AddRange(AuditFields.Where(a => !fields.Any(f => NameEquals(f.Name, a.Name))));

        var duplicate = fields
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            error = $"Duplicate field name '{duplicate.Key}'.";
            return false;
        }

        var columns = new List<ResolvedColumn>(fields.Count);
        foreach (var field in fields)
        {
            if (!ColumnTypeMapper.TryMap(field.Type, field.MaxLength, dialect, out var sqlType, out error))
            {
                error = $"Field '{field.Name}': {error}";
                return false;
            }

            // A primary key column is NOT NULL regardless of what the request said — the constraint
            // would reject NULLs anyway, so honouring nullable:true here would only produce DDL that
            // fails at execution time with a much worse message.
            columns.Add(new ResolvedColumn(
                field.Name,
                sqlType,
                Nullable: field.Nullable && !field.PrimaryKey,
                PrimaryKey: field.PrimaryKey,
                Unique: field.Unique,
                Indexed: field.Indexed));
        }

        resolved = new ResolvedTable(tableName, columns, hasTemplate ? template.Key : null);
        return true;
    }

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
