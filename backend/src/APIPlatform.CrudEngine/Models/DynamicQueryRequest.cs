namespace APIPlatform.CrudEngine.Models;

/// <summary>
/// A fully data-described read: which table, which columns, which equality filters — all supplied
/// by the caller at request time, not by any developer-authored repository or SQL. This is the
/// model behind <see cref="Interfaces.IDynamicQueryService"/>, the engine's answer to "reload user
/// data" style endpoints that used to hardcode a table/column shape per app (e.g. the old
/// LoginController.ReloadUserData): the engine only ever sees this description and a result set,
/// never a table or column name written into its own source.
/// </summary>
public sealed class DynamicQueryRequest
{
    /// <summary>Table (or view) to read from.</summary>
    public required string TableName { get; init; }

    /// <summary>Columns to select. Must name at least one column — there is no implicit "*"
    /// because the caller, not the engine, decides the result shape.</summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>Equality filters, ANDed together (e.g. { "DbName": "IQS_APP_DEV", "UserName": "jdoe" }).
    /// Values are always sent as SQL parameters. Empty means "no filter" — combined with <see cref="Top"/>
    /// this still returns at most a bounded page, never an unbounded table scan.</summary>
    public IReadOnlyDictionary<string, object?> Filters { get; init; } = new Dictionary<string, object?>();

    /// <summary>Row cap, clamped server-side to [1, 5000]. Defaults to 500 so an over-broad or
    /// missing filter can't accidentally pull an entire table back to the caller.</summary>
    public int Top { get; init; } = 500;
}
