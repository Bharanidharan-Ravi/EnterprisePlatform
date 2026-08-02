using Nucleus.SharedSchema.Models;

namespace APIPlatform.CrudEngine.Sql;

/// <summary>
/// Builds parameterized CRUD SQL from an EntityDefinition. Assumes every field is native to
/// the entity's own source — join-based relationship queries are deferred to Step 14
/// (APIPlatform.SchemaService).
/// </summary>
internal static class SqlQueryBuilder
{
    public static string SelectAll(EntityDefinition def) =>
        $"SELECT * FROM {def.SourceName}" + (def.IsTenantScoped ? " WHERE TenantId = @TenantId" : "");

    public static string SelectByKey(EntityDefinition def, IEnumerable<string> keyFieldNames) =>
        $"SELECT * FROM {def.SourceName} WHERE {WhereClause(keyFieldNames)}" + TenantAnd(def);

    public static string Insert(EntityDefinition def)
    {
        var fields = NativeFieldNames(def).ToList();
        var columns = string.Join(", ", fields);
        var parameters = string.Join(", ", fields.Select(f => $"@{f}"));
        return $"INSERT INTO {def.SourceName} ({columns}) VALUES ({parameters})";
    }

    public static string Update(EntityDefinition def, IEnumerable<string> keyFieldNames)
    {
        var keys = keyFieldNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var setClause = string.Join(", ", NativeFieldNames(def).Where(f => !keys.Contains(f)).Select(f => $"{f} = @{f}"));
        return $"UPDATE {def.SourceName} SET {setClause} WHERE {WhereClause(keys)}" + TenantAnd(def);
    }

    public static string Delete(EntityDefinition def, IEnumerable<string> keyFieldNames) =>
        $"DELETE FROM {def.SourceName} WHERE {WhereClause(keyFieldNames)}" + TenantAnd(def);

    private static IEnumerable<string> NativeFieldNames(EntityDefinition def) =>
        def.Fields.Where(f => f.SourcedViaRelationshipName is null).Select(f => f.Name);

    private static string WhereClause(IEnumerable<string> keyFieldNames) =>
        string.Join(" AND ", keyFieldNames.Select(k => $"{k} = @{k}"));

    private static string TenantAnd(EntityDefinition def) => def.IsTenantScoped ? " AND TenantId = @TenantId" : "";
}
