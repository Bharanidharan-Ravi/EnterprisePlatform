# APIPlatform.Database.Migration

A standalone, application-agnostic database schema deployment/versioning engine. It works
through the existing `APIPlatform.Database` connection/execution layer — it does **not** create a
second connection framework, and it never contains any module's business logic:

```text
Application
      ↓
APIPlatform.Database.Migration   (this package)
      ↓
APIPlatform.Database  (IDatabaseExecutor / IDatabaseTransaction)
      ↓
Dapper / ADO.NET
      ↓
SQL Server or SAP HANA
```

## What this package is responsible for

Only deploying/versioning database schema: discovering registered migrations, ordering them,
tracking which have already been applied, and applying the ones that haven't — nothing else. It
has no reference to `APIPlatform.Notification`, `APIPlatform.CrudEngine`, SignalR/Realtime, or
CrossCutting, and no application-specific or stored-procedure business logic. The one piece of
schema it owns for itself is its own bookkeeping table, `MigrationHistory`.

## Core contract

```csharp
public interface IMigration
{
    string MigrationId { get; }        // stable, provider-agnostic identity
    int Version { get; }               // ordering
    string Description { get; }
    DatabaseProvider SupportedProvider { get; }
    Task ApplyAsync(IDatabaseExecutor executor, IDatabaseTransaction? transaction, CancellationToken cancellationToken = default);
}
```

A logical migration that must exist on both SQL Server and SAP HANA is two `IMigration`
implementations sharing the same `MigrationId`/`Version` (one per provider, see
`Migrations/Notification/`) — `IMigrationRunner` only ever applies the one matching the
application's configured `DatabaseOptions.Provider`, and both are tracked as the same row in
history.

## Registration — an explicit step, never automatic

```csharp
services.AddSqlServerProvider();                 // or AddHanaProvider()
services.AddDatabase(options => configuration.GetSection("Database").Bind(options));
services.AddDatabaseMigration();                  // core engine — dialect resolver, history, runner
services.AddNotificationSchemaMigrations();        // opt in to the migrations you want deployed
```

`AddDatabaseMigration()` requires `AddDatabase(...)` (with a matching provider) and an `IClock`
registration to already be present, exactly like `AddNotification()` — it registers neither
itself. It also registers no `IMigration` — each schema-owning concern (Notification today, a
future stored-procedure package, an application's own tables) opts in with its own
`AddXxxMigrations()`-style extension, additive and independent of every other one.

Running migrations is always an explicit step your host chooses when to take:

```csharp
var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
var result = await runner.RunAsync(cancellationToken);
// result.Applied  — newly applied this run, in order
// result.Skipped  — already-applied ids that matched this provider and were left alone
```

Nothing in this package runs migrations automatically at host startup. A future transport layer
(CLI, HTTP/cURL endpoint) would only ever call `IMigrationRunner.RunAsync()` — the engine has no
transport dependency today, so that stays true without any change here.

## Idempotency and failure handling

A migration is identified by its `MigrationId` in the `MigrationHistory` table — running the same
set of migrations twice is a no-op the second time (everything comes back in `Skipped`), by
history-tracking, not by `IF NOT EXISTS` DDL. If a migration's `ApplyAsync` throws,
`IMigrationRunner.RunAsync` stops immediately and throws `MigrationException`, which carries
`FailedMigrationId` and `AppliedBeforeFailure` (everything that *did* succeed earlier in the same
run) — a caller always knows exactly how far a failed run got, never just "it failed somewhere."

## SQL Server / SAP HANA portability — and the one real asymmetry

Identifier quoting (`[x]` vs `"x"`) and a handful of portable column types (`DATETIME2(3)` vs
`TIMESTAMP`, `INT` vs `INTEGER`, `TABLE` vs `COLUMN TABLE`) are the only per-provider differences
in `MigrationHistory`'s own DDL, via `IMigrationSqlDialect` — the same "small dialect
abstraction at the call site" pattern `APIPlatform.Notification`/`APIPlatform.CrudEngine` use.
`MigrationHistory` existence is checked via `INFORMATION_SCHEMA.TABLES`, which both engines
expose, rather than an engine-specific catalog view.

**DDL transactionality is not symmetric between the two engines, and this package does not
pretend otherwise:** SQL Server supports transactional DDL, so `MigrationRunner` wraps a
migration's `ApplyAsync` and its `MigrationHistory` row insert in one transaction — a failure
partway rolls the whole migration back. **SAP HANA's DDL auto-commits regardless of any
surrounding transaction.** On HANA, `MigrationRunner` runs a migration's statements directly (no
transaction object is opened) and records history immediately after — a failure partway through a
HANA migration can leave earlier `CREATE` statements in that same migration already committed.
Keep HANA migrations as a single additive, one-time set of `CREATE`s (as
`NotificationHanaMigration` is) rather than something meant to be safely retried after a partial
failure.

No `IDENTITY`, `NEWID()`, `GETDATE()`, or `MERGE` anywhere in this package — `MigrationHistory`'s
`Id`/`AppliedOnUtc` are runner-generated (`Guid.NewGuid()`, injected `IClock`), matching every
other platform table's API-generated ids/timestamps.

## Known limitation

SAP HANA support is verified at the generated-SQL and provider-selection level only (see
`APIPlatform.Database.Migration.Tests`) — exercising it against a live HANA instance requires a
reachable HANA server and is out of scope for this package's unit tests, the same limitation
`APIPlatform.Database`'s own README documents for the same reason.

## What's deliberately not here yet

Stored-procedure deployment (`APIPlatform.Database.StoredProcedures`, a future package that would
register its own `IMigration`s through this same engine), a cURL/HTTP endpoint over
`IMigrationRunner`, SignalR, and CrossCutting.
