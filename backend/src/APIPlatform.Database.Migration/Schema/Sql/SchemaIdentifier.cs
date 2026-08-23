using System.Text.RegularExpressions;

namespace APIPlatform.Database.Migration.Schema.Sql;

/// <summary>
/// The single gate every caller-supplied table and column name passes through before it can
/// appear in generated DDL.
///
/// <para>This matters more here than anywhere else in the platform. Everywhere else, caller input
/// reaches the database only as a parameter value (<c>@Id</c>, <c>@Application</c>) — Dapper binds
/// it and no amount of quoting or escaping is the caller's problem. Identifiers cannot work that
/// way: no database lets you parameterize a table or column name, so a name from a request body
/// has to be concatenated into the statement text. That makes an allowlist the only real defense,
/// which is what this is — a name either matches a plain identifier exactly, or it is rejected and
/// no SQL is built at all. Quoting via <c>IMigrationSqlDialect.QuoteIdentifier</c> still happens on
/// top, but is treated as defense in depth, not as the primary control: <c>]</c> and <c>"</c> are
/// both outside the pattern, so neither quoting style can be broken out of.</para>
/// </summary>
internal static partial class SchemaIdentifier
{
    /// <summary>Letter or underscore, then letters/digits/underscores, 1–63 characters total —
    /// comfortably inside both SQL Server's 128-character and HANA's 127-character limits, with
    /// room for the engine's own <c>IX_{table}_{column}</c> index-name prefixes.</summary>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool IsValid(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && Pattern().IsMatch(identifier);

    /// <summary>Validates <paramref name="identifier"/>, returning false and a caller-ready
    /// message describing the rule it broke.</summary>
    public static bool TryValidate(string? identifier, string role, out string error)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            error = $"{role} name is required.";
            return false;
        }

        if (!Pattern().IsMatch(identifier))
        {
            error = $"{role} name '{identifier}' is not a valid identifier — use only letters, " +
                    "digits, and underscores, start with a letter or underscore, and keep it to 63 characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
