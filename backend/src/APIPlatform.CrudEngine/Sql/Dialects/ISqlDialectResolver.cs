using APIPlatform.Data.Options;
using Microsoft.Extensions.Options;

namespace APIPlatform.CrudEngine.Sql.Dialects;

/// <summary>Maps the app's configured DatabaseProvider (APIPlatform.Data, frozen) to an
/// ISqlDialect, so the query pipeline never branches on provider itself (Req 11).</summary>
public interface ISqlDialectResolver
{
    ISqlDialect Resolve();
}

/// <summary>
/// Phase 2 fix: previously took a raw <see cref="DatabaseOptions"/> constructor parameter, but
/// APIPlatform.Data's AddDatabase() only ever registers it through the standard
/// <see cref="IOptions{TOptions}"/> pattern (services.Configure(...)) — nothing registers a bare
/// DatabaseOptions singleton. Combining AddDatabase() with AddCrudEngine() in the same container
/// (never previously exercised anywhere in this repo before Phase 2's Employee wiring) failed to
/// resolve this type at runtime as a result. Depending on IOptions&lt;DatabaseOptions&gt; instead
/// matches how every other APIPlatform.Data consumer is expected to read these options.
/// </summary>
public sealed class DefaultSqlDialectResolver : ISqlDialectResolver
{
    private readonly DatabaseOptions _options;

    public DefaultSqlDialectResolver(IOptions<DatabaseOptions> options) => _options = options.Value;

    public ISqlDialect Resolve() => _options.Provider switch
    {
        DatabaseProvider.SqlServer => new SqlServerDialect(),
        DatabaseProvider.Hana => new HanaDialect(),
        _ => new SqlServerDialect()
    };
}
