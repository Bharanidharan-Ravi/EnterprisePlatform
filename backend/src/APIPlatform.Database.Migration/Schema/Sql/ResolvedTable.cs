namespace APIPlatform.Database.Migration.Schema.Sql;

/// <summary>One fully-resolved column: a validated identifier and a provider-specific SQL type,
/// with nothing left to interpret. Everything downstream of
/// <see cref="TableDefinitionResolver"/> works on these rather than on caller input.</summary>
internal sealed record ResolvedColumn(
    string Name,
    string SqlType,
    bool Nullable,
    bool PrimaryKey,
    bool Unique,
    bool Indexed);

/// <summary>A table request that has passed validation — physical name plus ordered columns.</summary>
internal sealed record ResolvedTable(
    string TableName,
    IReadOnlyList<ResolvedColumn> Columns,
    string? TemplateKey);
