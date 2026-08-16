# APIPlatform.Notification — V1

Use the **API Platform Master Engineering Context** as the architectural authority.

Build `APIPlatform.Notification` as a **standalone, reusable, application-agnostic notification module**.

## Goal

Create one common notification engine that can be reused by IQS, Nucleus, CRM, Project, Ticketing, HRMS, and future applications.

The module must support:

* Notification to all users
* Notification to individual users
* Notification to groups
* Multiple users/groups
* Target + exclusion rules
* Application context
* Entity context (`EntityType` + `EntityId`)
* Event/action type
* Persistent notifications
* User-specific unread/read state
* Efficient unread count
* Notification history
* Extensible delivery mechanisms
* Future realtime delivery through the separate `APIPlatform.Realtime` module

## Important Architecture

Keep Notification completely independent from:

* IQS
* Ticketing
* Project
* Nucleus
* CRM
* SignalR implementation
* CrossCutting implementation

Notification must depend only on platform abstractions such as `APIPlatform.Database`.

Do NOT implement SignalR inside this module.

Do NOT implement CrossCutting inside this module.

## Data Design

Prefer a minimal, high-performance model.

Start by evaluating:

1. `Notification`
2. `NotificationTarget`
3. `NotificationUserState`

Do NOT create one recipient row for every user × notification unless technically necessary.

Resolve group membership dynamically.

Support target/exclusion semantics without hardcoding users.

Use API-generated `NVARCHAR(36)` IDs and API-generated timestamps, consistent with the platform database architecture.

The database design must remain SQL Server + SAP HANA compatible.

## Read Model

Prefer efficient user-level state such as `LastReadOn`/equivalent for the normal case rather than materializing every notification/user relationship.

If exact per-notification read/unread state is required, design it as an explicit extension rather than sacrificing scalability by default.

Clearly distinguish synchronization state from read state.

## API Design

Create clean abstractions such as:

* `INotificationService`
* `INotificationRepository`
* recipient/target resolution abstractions where appropriate

Keep interfaces small and responsibility-focused.

The consuming application should be able to express something conceptually like:

```text
Create notification
Application = PROJECT
Entity = PROJECT / PRJ001
Event = PROJECT_CREATED
Target = GROUP / PROJECT_TEAM
Exclude = USER / USER007
```

The notification module decides how to persist and resolve notification state, not the business application.

## Quality Requirements

* Production-grade
* DRY
* SOLID
* Async
* CancellationToken where appropriate
* Proper transaction handling
* Safe concurrency behavior
* Proper indexes
* Efficient unread-count queries
* No N+1 queries
* No hardcoded users/groups
* No application-specific business logic
* No duplicated database logic
* No hidden failures
* No unnecessary abstractions

## Implementation Process

FIRST inspect the existing API Platform architecture and `APIPlatform.Database`.

Then design the Notification schema and contracts.

Do not immediately start coding.

Identify:

* tables
* relationships
* indexes
* constraints
* repository contracts
* service contracts
* DTOs
* recipient resolution strategy
* read/unread strategy
* transaction boundaries
* SQL Server/HANA compatibility concerns

Present the proposed design and important tradeoffs first.

After approval, implement it.

Finally:

* build the solution
* run tests
* add meaningful unit/integration tests where possible
* verify SQL Server compatibility
* verify HANA-compatible SQL
* document the public API and usage
* report all files changed and test results

Do not implement SignalR or CrossCutting yet.
