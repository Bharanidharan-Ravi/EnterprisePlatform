using APIPlatform.Data.Options;

namespace APIPlatform.CrudEngine.Sql.Dialects;

/// <summary>Maps the app's configured DatabaseProvider (APIPlatform.Data, frozen) to an
/// ISqlDialect, so the query pipeline never branches on provider itself (Req 11).</summary>
public interface ISqlDialectResolver
{
    ISqlDialect Resolve();
}

/// <summary>ASSUMPTION BOUNDARY: assumes DatabaseOptions exposes a `Provider` property of type
/// DatabaseProvider (per your Step 2 summary: "Options/ (DatabaseProvider enum, DatabaseOptions)").
/// Only this class needs adjusting if the real property name differs.</summary>
public sealed class DefaultSqlDialectResolver : ISqlDialectResolver
{
    private readonly DatabaseOptions _options;

    public DefaultSqlDialectResolver(DatabaseOptions options) => _options = options;

    public ISqlDialect Resolve() => _options.Provider switch
    {
        DatabaseProvider.SqlServer => new SqlServerDialect(),
        DatabaseProvider.Hana => new HanaDialect(),
        _ => new SqlServerDialect()
    };
}
