# APIPlatform.Notification

A standalone, application-agnostic notification engine, reusable by IQS, Nucleus, CRM, Project,
Ticketing, HRMS, and future applications. Depends only on `APIPlatform.Foundation` (`IClock`,
`Result<T>`/`OperationResult`/`PagedResult<T>`) and `APIPlatform.Database`. No reference to any
application project, `APIPlatform.CrossCutting`, or SignalR/`APIPlatform.Realtime` — realtime
delivery is a separate, future concern layered on top of this module, not inside it.

## Data model

Three tables, deliberately **not** one row per (notification × recipient):

- **Notifications** — one row per notification event (`Application`, optional `EntityType`/`EntityId`,
  `EventType`, `Title`, `Message`, an opaque `Data` JSON payload, `CreatedBy`, `CreatedOnUtc`).
- **NotificationTargets** — declarative target/exclusion rules per notification (`TargetKind`:
  All/User/Group, `TargetValue`, `IsExclusion`). Group *membership* is never materialized here —
  only the group *code* the notification was addressed to.
- **NotificationUserStates** — one row per `(UserId, Application)` holding `LastReadOnUtc` and
  `LastSyncedOnUtc`. Read state and sync state are intentionally separate columns: a client can
  observe new notifications (sync) without the user having acknowledged them (read).

Reference DDL for both supported engines lives under `Schema/SqlServer` and `Schema/Hana` — apply
it via your application's own deployment process; the platform has no migration runner yet.
IDs are API-generated `NVARCHAR(36)` GUIDs and timestamps are API-generated (`IClock.UtcNow`) —
no `IDENTITY`, `NEWID()`, or `GETDATE()`/`CURRENT_TIMESTAMP` defaults anywhere in the schema.

**Deferred extension:** exact per-notification read/unread state (`NotificationReadReceipt`) is
not implemented — `NotificationUserStates` gives "how many are unread since X" in O(1) storage per
user, which is what an inbox badge needs. A consumer that genuinely needs to know *which specific*
notifications are read should add that as its own table, not by changing this module's default.

## Recipient resolution — read this before wiring in a new application

Notification **never resolves group membership**. `IQS`'s notion of a "team" and `Project`'s
notion of a "project group" are different, application-owned concepts — baking either into this
module would violate its one job (be reusable by all of them). Instead, every read-side call
takes a `NotificationRecipient { UserId, GroupCodes }`: **the calling application resolves the
current user's group codes from its own RBAC/org data and passes them in.** Notification then
does a single indexed SQL query (`EXISTS`/`NOT EXISTS` against `NotificationTargets`, with the
group codes as one parameterized `IN` list) — no N+1, and no per-app plugin interface to register.

```csharp
// Somewhere the app already knows this — e.g. from its own RBAC/team service.
var recipient = NotificationRecipient.For(userId, groupCodes: ["PROJECT_TEAM", "PROJECT_LEADS"]);

var unread = await notificationService.GetUnreadCountAsync("PROJECT", recipient, ct);
var page = await notificationService.GetNotificationsAsync("PROJECT", recipient, pageNumber: 1, pageSize: 20, ct);
```

## Creating a notification

```csharp
var request = new CreateNotificationRequest
{
    Application = "PROJECT",
    EntityType = "PROJECT",
    EntityId = "PRJ001",
    EventType = "PROJECT_CREATED",
    Title = "Project PRJ001 was created",
    CreatedBy = currentUserId,
    Targets =
    [
        NotificationTargetRule.TargetGroup("PROJECT_TEAM"),
        NotificationTargetRule.ExcludeUser("USER007")
    ]
};

var result = await notificationService.CreateAsync(request, ct);
```

`CreateAsync` validates the request (required fields, at least one non-exclusion target rule,
consistent `EntityType`/`EntityId` pairing) and returns `Result<NotificationRecord>` — validation
failures never throw, they come back as `ErrorInfo` entries. The notification row and all of its
target rows are inserted in a single transaction: a notification is never left persisted without
its targets.

## Read/unread and sync

```csharp
await notificationService.MarkAsReadAsync("PROJECT", userId, ct: ct);    // upToUtc defaults to now
await notificationService.MarkAsSyncedAsync("PROJECT", userId, ct: ct);  // independent of read state
```

Both are implemented as an update-first, insert-on-first-touch write to `NotificationUserStates`
(no `MERGE`, for SQL Server/HANA portability). On the rare race where two first-ever calls for the
same `(UserId, Application)` both miss the `UPDATE` and both attempt the `INSERT`, the loser's
insert fails and is retried as an `UPDATE` once (the winner's row now exists) — a genuine
infrastructure failure still propagates if that retry also affects zero rows, so this never
silently swallows a real error.

## Registration

```csharp
services.AddSqlServerProvider(); // or AddHanaProvider()
services.AddDatabase(options => configuration.GetSection("Database").Bind(options));
services.AddNotification();
```

`AddNotification()` requires `AddDatabase(...)` (with a matching provider) and an `IClock`
registration to already be present — it registers neither itself, since both are shared platform
concerns owned by their own modules.

## SQL Server / SAP HANA portability

- Both engines are covered by one shared query-generation path (`NotificationSqlBuilder`); only
  identifier quoting (`[x]` vs `"x"`) and paging (`OFFSET/FETCH` vs `LIMIT/OFFSET`) branch by
  provider, via a small internal dialect pair — the same pattern `APIPlatform.CrudEngine` uses,
  kept independent here so Notification never references CrudEngine.
- No `MERGE`, `OUTPUT INSERTED`, table-valued parameters, or SQL Server-only JSON/XML functions.
- The two schema files differ only in column *types* (`NVARCHAR(MAX)`→`NCLOB`, `DATETIME2`→`TIMESTAMP`,
  `BIT`→`BOOLEAN`), never in table shape, keys, or indexes.
