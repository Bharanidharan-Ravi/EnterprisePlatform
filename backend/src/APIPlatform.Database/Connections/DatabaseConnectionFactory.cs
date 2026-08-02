using System.Data;
using Microsoft.Extensions.Options;
using APIPlatform.Data.Exceptions;
using APIPlatform.Data.Options;
using APIPlatform.Data.Providers;
using APIPlatform.Foundation.Exceptions;

namespace APIPlatform.Data.Connections;

/// <summary>Default IDatabaseConnectionFactory — resolves the configured provider and opens a connection through it.</summary>
public sealed class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly IDatabaseProviderFactory _providerFactory;

    public DatabaseConnectionFactory(IOptions<DatabaseOptions> options, IDatabaseProviderFactory providerFactory)
    {
        _options = options.Value;
        _providerFactory = providerFactory;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetProvider(_options.Provider);
        var connection = provider.CreateConnection(_options.ConnectionString);
        try
        {
            if (connection is System.Data.Common.DbConnection dbConnection)
                await dbConnection.OpenAsync(cancellationToken);
            else
                connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Failed to open a {_options.Provider} connection.", ex, ErrorCategory.Infrastructure);
        }
    }
}
