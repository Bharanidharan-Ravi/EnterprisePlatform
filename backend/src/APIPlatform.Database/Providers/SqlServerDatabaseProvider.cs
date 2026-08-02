using System.Data;
using Microsoft.Data.SqlClient;
using APIPlatform.Data.Options;

namespace APIPlatform.Data.Providers;

/// <summary>Default provider, targeting SQL Server (and SAP HANA-compatible T-SQL surfaces where applicable).</summary>
public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public DatabaseProvider Kind => DatabaseProvider.SqlServer;

    public IDbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
}
