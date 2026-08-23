using APIPlatform.Database.Migration.Sql.Dialects;

namespace APIPlatform.Database.Migration.Schema.Sql;

/// <summary>
/// Maps a <see cref="Models.FieldDefinition.Type"/> name onto the configured provider's real
/// column type. The set of accepted names is closed and defined here — a caller can never supply
/// a type expression that reaches the database verbatim, only choose from this list, which is
/// what keeps the same request body meaningful on both SQL Server and SAP HANA.
/// </summary>
internal static class ColumnTypeMapper
{
    /// <summary>Length used for a <c>string</c> field that does not specify one.</summary>
    public const int DefaultStringLength = 200;

    /// <summary>Upper bound for <c>string</c>; longer text should use <c>text</c>, which maps to
    /// NVARCHAR(MAX)/NCLOB. 4000 is the largest non-MAX NVARCHAR SQL Server allows.</summary>
    public const int MaxStringLength = 4000;

    public static readonly IReadOnlyList<string> SupportedTypes =
        ["string", "text", "int", "long", "bool", "datetime", "decimal", "guid", "json"];

    public static bool TryMap(string? type, int? maxLength, IMigrationSqlDialect dialect, out string sqlType, out string error)
    {
        sqlType = string.Empty;
        error = string.Empty;

        var normalized = (type ?? "string").Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "string":
                var length = maxLength ?? DefaultStringLength;
                if (length is < 1 or > MaxStringLength)
                {
                    error = $"maxLength must be between 1 and {MaxStringLength} for a 'string' field " +
                            $"(got {length}); use type 'text' for unbounded text.";
                    return false;
                }
                sqlType = dialect.StringType(length);
                return true;

            // Both map to the provider's unbounded text type; 'json' is a separate name purely so a
            // request reads as intent ("this column holds a JSON document") — neither engine has a
            // native JSON column type that APIPlatform.Data reads back differently.
            case "text":
            case "json":
                sqlType = dialect.UnboundedTextType;
                return true;

            case "int":
                sqlType = dialect.IntegerType;
                return true;

            case "long":
                sqlType = dialect.BigIntType;
                return true;

            case "bool":
                sqlType = dialect.BooleanType;
                return true;

            case "datetime":
                sqlType = dialect.TimestampType;
                return true;

            case "decimal":
                sqlType = dialect.DecimalType;
                return true;

            // Stored as text, not a native UNIQUEIDENTIFIER: the platform generates ids in the API
            // and stores them as NVARCHAR(36) everywhere (MigrationHistory, Notifications), so a
            // 'guid' field matches that existing convention rather than introducing a second one.
            case "guid":
                sqlType = dialect.StringType(36);
                return true;

            default:
                error = $"Unknown field type '{type}'. Supported types: {string.Join(", ", SupportedTypes)}.";
                return false;
        }
    }
}
