using System.Data;

namespace APIPlatform.Data.Connections;

/// <summary>Creates open database connections using the configured provider and connection string.</summary>
public interface IDatabaseConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
