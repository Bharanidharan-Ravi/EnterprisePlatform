# APIPlatform.Database.Tests

Unit tests for `APIPlatform.Data` covering provider resolution, DI registration, configuration
binding, and the provider-neutrality of the transaction/executor abstractions.

## Scope

These tests run without any database server — SQL Server or SAP HANA — reachable. They verify:

- `SqlServerDatabaseProvider` / `HanaDatabaseProvider` report the correct `DatabaseProvider.Kind`
  and construct the correct ADO.NET connection type (`SqlConnection` / `HanaConnection`).
- `DatabaseProviderFactory` resolves the provider matching a configured `DatabaseProvider`, and
  throws `DatabaseException` (not a generic/silent failure) for one that isn't registered.
- `AddDatabase()` + `AddSqlServerProvider()` / `AddHanaProvider()` resolve `IDatabaseExecutor`,
  `IDatabaseConnectionFactory`, and `IStoredProcedureExecutor` for either engine, with connection
  scope/lifetime intact (no accidental singleton connection).
- `Database:Provider` / `Database:ConnectionString` configuration binds to `DatabaseOptions`
  correctly for both `"SqlServer"` and `"Hana"`.
- `IDatabaseTransaction` and `IDatabaseExecutor` expose no SqlClient- or HANA-specific type on
  their public surface, and `SqlDatabaseExecutor` holds no `SqlConnection`/`HanaConnection`-typed
  field — the same executor genuinely runs against either engine.

## What's intentionally not here

**Live integration tests against a real SQL Server or SAP HANA instance are not included.**
Faking one (e.g. asserting against a mocked `IDbConnection`) would not prove anything about the
real `Sap.Data.Hana.Net` provider or `Microsoft.Data.SqlClient` behavior, so no such test is
provided rather than a misleading one. Verifying `QueryAsync`/`ExecuteAsync`/transactions/stored
procedures end-to-end against SAP HANA requires a reachable HANA server (or HANA Cloud trial
instance) with a real connection string supplied out-of-band (e.g. via environment variable or
user secrets, never committed) — that is a deployment/environment concern, not something this
test project should assume.
