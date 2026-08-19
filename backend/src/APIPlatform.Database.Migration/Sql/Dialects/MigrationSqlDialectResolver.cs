using APIPlatform.Data.Options;
using Microsoft.Extensions.Options;

namespace APIPlatform.Database.Migration.Sql.Dialects;

/// <summary>Maps the app's configured <see cref="DatabaseProvider"/> (APIPlatform.Data) to an
/// <see cref="IMigrationSqlDialect"/>, so no other type in this package branches on provider
/// itself. Mirrors APIPlatform.Notification's NotificationSqlDialectResolver exactly.</summary>
public interface IMigrationSqlDialectResolver
{
    IMigrationSqlDialect Resolve();
}

internal sealed class MigrationSqlDialectResolver : IMigrationSqlDialectResolver
{
    private readonly DatabaseOptions _options;

    public MigrationSqlDialectResolver(IOptions<DatabaseOptions> options) => _options = options.Value;

    public IMigrationSqlDialect Resolve() => _options.Provider switch
    {
        DatabaseProvider.SqlServer => new SqlServerMigrationDialect(),
        DatabaseProvider.Hana => new HanaMigrationDialect(),
        _ => new SqlServerMigrationDialect()
    };
}
