using System.Data;
using Microsoft.Data.SqlClient;
using APIPlatform.Data.Options;

namespace APIPlatform.Data.Providers;

/// <summary>Microsoft SQL Server provider — creates connections via Microsoft.Data.SqlClient.</summary>
public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public DatabaseProvider Kind => DatabaseProvider.SqlServer;

    public IDbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
}
