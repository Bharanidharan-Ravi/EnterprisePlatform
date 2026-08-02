using System.Data;
using APIPlatform.Data.Options;

namespace APIPlatform.Data.Providers;

/// <summary>
/// Strategy contract for a single database engine. Adding a new engine (Sqlite, PostgreSql,
/// Oracle, etc.) means adding a new implementation of this interface only — no changes to
/// any other public contract in the package.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Which engine this provider implements.</summary>
    DatabaseProvider Kind { get; }

    /// <summary>Creates an unopened connection for the given connection string.</summary>
    IDbConnection CreateConnection(string connectionString);
}
