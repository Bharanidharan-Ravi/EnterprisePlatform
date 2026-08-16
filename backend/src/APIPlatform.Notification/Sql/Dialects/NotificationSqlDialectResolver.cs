using APIPlatform.Data.Options;
using Microsoft.Extensions.Options;

namespace APIPlatform.Notification.Sql.Dialects;

/// <summary>Maps the app's configured <see cref="DatabaseProvider"/> (APIPlatform.Data) to an
/// <see cref="INotificationSqlDialect"/>, so the repository never branches on provider itself.</summary>
public interface INotificationSqlDialectResolver
{
    INotificationSqlDialect Resolve();
}

internal sealed class NotificationSqlDialectResolver : INotificationSqlDialectResolver
{
    private readonly DatabaseOptions _options;

    public NotificationSqlDialectResolver(IOptions<DatabaseOptions> options) => _options = options.Value;

    public INotificationSqlDialect Resolve() => _options.Provider switch
    {
        DatabaseProvider.SqlServer => new SqlServerNotificationDialect(),
        DatabaseProvider.Hana => new HanaNotificationDialect(),
        _ => new SqlServerNotificationDialect()
    };
}
