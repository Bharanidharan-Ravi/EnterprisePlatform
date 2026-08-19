# Notification Verification + Database Migration Foundation

Use the API Platform Master Engineering Context and the existing `APIPlatform.Notification` implementation as the source of truth.

## Phase 1 — Verify Notification

Before changing anything, inspect the complete `APIPlatform.Notification` implementation.

Verify that the implementation matches its existing README/design:

* Notification
* NotificationTargets
* NotificationUserStates
* ALL / USER / GROUP targets
* Exclusions
* Application + Entity context
* Read state
* Sync state
* Unread count
* Pagination
* Recipient filtering
* Transaction handling
* Concurrency handling
* SQL Server + SAP HANA compatibility

Pay special attention to:

* exclusion always overriding inclusion
* duplicate recipients when multiple groups match
* `LastReadOnUtc` vs `LastSyncedOnUtc`
* notifications created exactly at the read cursor
* inactive notifications
* pagination correctness
* unread-count correctness
* SQL injection safety
* no N+1 queries
* correct indexes
* SQL Server/HANA SQL dialect differences

Do not redesign working architecture without a concrete reason.

Add/fix tests where required.

Run:

* full build
* Notification tests
* database-related tests
* existing consumer build/tests where applicable

Report all results.

---

# Phase 2 — Database Migration Foundation

Create a separate reusable platform package for database schema deployment/migration.

Suggested conceptual name:

`APIPlatform.Database.Migration`

This package is responsible ONLY for deploying/versioning database schema.

It must NOT contain:

* Notification business logic
* Notification repositories/services
* SignalR
* CrossCutting
* application-specific business logic
* stored-procedure business logic

## Database Connection

The migration package must use the application's existing database configuration and connection abstraction.

The application provides:

```text
Database.Provider
Database.ConnectionString
```

The migration package must work through the existing:

```text
APIPlatform.Database
        ↓
Dapper / ADO.NET
        ↓
SQL Server or SAP HANA
```

Do NOT create a second database connection framework.

Do NOT hardcode connection strings.

---

# Phase 3 — Migration Structure

Create a clean versioned migration structure, for example:

```text
APIPlatform.Database.Migration
│
├── Abstractions
├── Models
├── Services
├── Providers/Dialects
├── Migrations
│   ├── SqlServer
│   └── Hana
└── DependencyInjection
```

Adapt the structure to the existing solution instead of blindly copying it.

Create a migration contract capable of:

```text
MigrationId
Version
Description
SupportedProvider
Up/Apply
```

The migration runner must know which migrations have already been applied.

Create a minimal migration history table.

The migration history mechanism must itself be compatible with SQL Server and SAP HANA.

Do not use:

* `IDENTITY`
* `NEWID()`
* `GETDATE()`
* `MERGE`
* SQL Server-only features

---

# Phase 4 — Notification Schema Migration

Create the initial Notification database migration for BOTH providers:

```text
SQL Server
SAP HANA
```

The migration must create all tables required by the current Notification implementation.

At minimum verify:

```text
Notifications
NotificationTargets
NotificationUserStates
```

Include:

* primary keys
* foreign keys where appropriate
* unique constraints
* indexes required by the actual notification queries
* application/entity indexes
* target lookup indexes
* user/application state indexes
* timestamps
* active/status fields where required by the implementation

Do NOT invent additional tables unless the implementation actually requires them.

Do NOT add per-user-per-notification recipient rows.

Do NOT add `NotificationReadReceipt` yet.

---

# Phase 5 — Use Application Database

When an application consumes Notification, the migration should be executable using that application's configured database.

Conceptually:

```text
IQS API
   ↓
Database Connection String
   ↓
APIPlatform.Database.Migration
   ↓
Create Notification tables
   ↓
IQS Database
```

Later:

```text
Nucleus API
   ↓
Nucleus Connection String
   ↓
Same migration package
   ↓
Nucleus Database
```

The platform must NOT assume a centralized Notification database unless explicitly configured by the application.

The default model is:

> Notification tables live in the consuming application's database.

---

# Phase 6 — Stored Procedures

DO NOT implement the Notification stored-procedure package in this task.

A separate package will later handle:

```text
APIPlatform.Database.StoredProcedures
```

which will support:

* SQL Server
* SAP HANA

For now, the migration foundation only needs to provide a clean extension point so future migrations can deploy stored procedures independently.

Do not duplicate stored-procedure logic inside Notification.

---

# Phase 7 — Future cURL / HTTP Support

Do not build the cURL/HTTP feature now.

However, design the migration package so it can later support a model such as:

```text
Application
    ↓
Migration API / CLI / cURL
    ↓
APIPlatform.Database.Migration
    ↓
Application Database
```

Keep the core migration engine independent from transport.

The future HTTP/cURL layer should only invoke the migration service; it must not contain migration logic.

---

# Phase 8 — DI

Provide clean registration, conceptually:

```csharp
services.AddDatabase(...);
services.AddDatabaseMigration(...);
services.AddNotification(...);
```

Do not make Notification responsible for running migrations automatically unless there is already an established platform convention.

Prefer an explicit migration execution step.

---

# Phase 9 — Testing

Add tests for:

* migration discovery
* migration ordering
* migration history
* already-applied migration handling
* SQL Server migration generation/execution path
* HANA migration generation/execution path
* provider selection
* idempotency
* failure handling
* rollback/transaction behavior where supported
* Notification schema compatibility with the actual repository queries

If no live HANA database exists, do NOT fake a HANA integration test.

Test the generated HANA SQL/schema and provider path, and document the live-HANA test limitation.

---

# Phase 10 — Final Validation

After implementation:

1. Build the entire solution.
2. Run all existing tests.
3. Run Notification tests.
4. Run migration tests.
5. Build a real consumer such as IQS/Playground.
6. Verify the migration package does not introduce application-specific dependencies.
7. Verify SQL Server compatibility.
8. Verify HANA compatibility.
9. Verify Dapper/database abstraction reuse.
10. Verify no duplicated database connection infrastructure.
11. Verify no Notification business logic leaked into migration.
12. Report every changed/created file.

## Important

Do NOT proceed to SignalR integration.

Do NOT implement CrossCutting.

Do NOT implement the future stored-procedure package.

Do NOT implement cURL/HTTP yet.

The final outcome of this task should be:

```text
APIPlatform.Database                  ✅
APIPlatform.Notification              ✅ verified
APIPlatform.Database.Migration        ✅ foundation
Notification SQL Server schema        ✅
Notification HANA schema              ✅
Stored procedure package              ⏳ future
cURL/HTTP migration endpoint           ⏳ future
SignalR integration                    ⏳ future
CrossCutting                           ⏳ future
```

Keep the migration engine small, provider-agnostic, reusable, idempotent, testable, and independent from all application/business logic.
