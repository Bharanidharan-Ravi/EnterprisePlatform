using System.Data;
using Sap.Data.Hana;
using APIPlatform.Data.Options;

namespace APIPlatform.Data.Providers;

/// <summary>
/// SAP HANA provider — creates connections via the official SAP HANA ADO.NET Data Provider
/// (Sap.Data.Hana.Net, published by SAP). HanaConnection is confined to this file; every other
/// type in the package (DatabaseConnectionFactory, SqlDatabaseExecutor, StoredProcedureExecutor,
/// consumers) only ever sees it as an IDbConnection.
/// </summary>
public sealed class HanaDatabaseProvider : IDatabaseProvider
{
    public DatabaseProvider Kind => DatabaseProvider.Hana;

    public IDbConnection CreateConnection(string connectionString) => new HanaConnection(connectionString);
}
