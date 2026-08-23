using System.Linq;
using System.Text.RegularExpressions;

namespace APIPlatform.CrudEngine.Sql;

/// <summary>
/// Shared identifier allow-list for the Dynamic* services (Query, Command). Table/column names
/// there arrive on the request itself rather than from developer-authored EntityDefinition config,
/// so — unlike SqlQueryBuilder's trusted-config path — they must be validated before they can be
/// placed directly into generated SQL text. Values are never affected by this; they always travel
/// as SQL parameters.
/// </summary>
internal static class SqlIdentifierValidator
{
    private static readonly Regex Pattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value);

    /// <summary>
    /// Same allow-list, applied to a table name that may be database/schema-qualified
    /// (<c>table</c>, <c>schema.table</c>, or <c>database.schema.table</c>) — e.g. a cross-database
    /// read like <c>IQS_DB.dbo.SEC_USER</c> against a connection whose catalog is something else.
    /// Every dot-separated part is checked individually; the string is still never anything but
    /// those validated parts rejoined by literal dots, so this stays as safe as <see cref="IsValid"/>.
    /// </summary>
    public static bool IsValidQualifiedName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split('.');
        return parts.Length is >= 1 and <= 3 && parts.All(IsValid);
    }
}
