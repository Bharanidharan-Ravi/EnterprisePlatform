# EnterprisePlatform — Platform Capability & Developer Usage Manual

*Code-grounded audit · not a redesign.* Every claim below was checked against the repository itself — grep, file reads, `dotnet build`, `dotnet test` — not against README promises or folder names. Where code and documentation disagree, the disagreement is stated, not resolved.

Audited 2026-08-20 · `d:\Project\EnterprisePlatform` · backend @ net10.0 · frontend @ React 19 / TypeScript. No source files were modified in the course of this audit.

**Status legend used throughout:** `COMPLETE` · `PARTIAL` · `FOUNDATION_ONLY` · `STUB` · `EXPERIMENTAL` · `APPLICATION_SPECIFIC` · `NOT_IMPLEMENTED` · `UNKNOWN` · `UNCONSUMED` (built but nothing imports it).

---

## Table of Contents

**Overview** — [1. Executive Summary](#1-executive-summary) · [2. Platform Vision](#2-platform-vision) · [3. Repository Inventory](#3-repository-inventory) · [4. Actual Architecture](#4-actual-architecture)

**APIPlatform** — [5. Overview](#5-apiplatform-overview) · [6. Capability Matrix](#6-apiplatform-capability-matrix) · [7. Request Lifecycle](#7-api-request-lifecycle) · [8. Database Platform](#8-database-platform) · [9. CRUD Engine](#9-crud-engine) · [10. Query / Search Engine](#10-query--search-engine) · [11. Authentication](#11-authentication) · [12. Authorization](#12-authorization) · [13. SignalR](#13-signalr) · [14. Notifications](#14-notifications) · [15. Storage / Documents](#15-storage--documents) · [16. Workflow](#16-workflow)

**UIPlatform** — [17. Overview](#17-uiplatform-overview) · [18. Capability Matrix](#18-ui-capability-matrix) · [19. Routing](#19-routing) · [20. Forms](#20-forms) · [21. Grids](#21-grids) · [22. State Management](#22-state-management) · [23. API Client / Data Flow](#23-api-client--ui-data-flow)

**Cross-cutting** — [24. Shared Schema](#24-shared-schema) · [25. Configuration](#25-configuration) · [26. Playground Validation](#26-playground-validation)

**Building on EP** — [27. Application Integration Manual](#27-application-integration-manual) · [28. Integration Patterns](#28-integration-patterns) · [29. Developer Cookbook](#29-developer-cookbook) · [30. Business Logic Boundaries](#30-business-logic-boundaries)

**Quality & Readiness** — [31. Testing](#31-testing) · [32. Security](#32-security) · [33. Performance](#33-performance) · [34. Provider Portability](#34-provider-portability) · [35. Production Readiness](#35-production-readiness)

**Roadmap** — [36. Remaining Work](#36-remaining-work) · [37. Missing Capabilities](#37-missing-capabilities) · [38. Technical Debt](#38-technical-debt) · [39. Recommended Next Phase](#39-recommended-next-phase) · [40. 10-Year Evolution](#40-10-year-evolution)

**Reference** — [41. Complete Capability Map](#41-complete-capability-map) · [42. File / Code Reference](#42-file--code-reference) · [43. Final Platform Status](#43-final-platform-status)

---

## 1. Executive Summary

EnterprisePlatform is, today, a small number of genuinely well-built backend modules, a much larger number of empty planned folders, and three solid but completely unconsumed React packages. Nothing in the repository currently exercises a real business entity end-to-end — no controller-to-database CRUD flow runs anywhere. What does run is narrower and more solid than the README implies: authentication, raw Dapper database access, schema migration, and notification persistence, all proven by a passing test suite and a green build.

> ### CURRENT PLATFORM STATUS
>
> **APIPlatform** — Implemented: Database access (Dapper/SQL Server/SAP HANA), Database.Migration, Authentication (login/JWT/password hashing), Notification (persistence layer). Partial: Authentication refresh flow (built but non-functional hand-off), Authorization/Rbac (complete in isolation, not wired to any host), Validation (working pipeline, zero validators). Foundation only: Foundation itself (contracts with no implementations), CrudEngine design (well-modeled, does not compile). Missing: AI, Cache, Diagnostics, Integration, Numbering, Reporting, SAP, Scheduler, Search, Security, SignalR, Storage, Sync, Workflow — 14 folders, zero files each.
>
> **UIPlatform** — Implemented: ui-platform-foundation (axios client, TanStack Query wrappers, Zustand store factory). Partial: ui-platform-auth (real login/session/guard logic, but two of three backend endpoints it calls don't exist or don't match), ui-platform-forms (real React Hook Form + Zod engine, genuinely schema-aware, but its own declared dependency on foundation is never imported). Missing: 17 of 20 package folders are empty, including grid, routing, signalr, notification, storage, workflow, dashboard, calendar, editor.
>
> **Shared Schema** — Implemented: 6 model classes + 3 enums exist as loose C# files (`nucleus/shared/Nucleus.SharedSchema`). Missing: no `.csproj`, not in the solution, not referenced by any working build — the one module that needs it (CrudEngine) points at a path that doesn't exist.
>
> **Playground** — Validated: Foundation, Shared, Logging, Configuration, Validation, Database, Database.Migration, Authentication (8 project references, builds and runs). Not validated: CrudEngine, Authorization, Notification, and all 14 empty modules — none are referenced by the playground at all.
>
> **Overall** — Current maturity: **Foundation**, with one corner (auth + Dapper data access + migrations) approaching **Developer Preview**. Biggest strength: the data-access and migration layers are genuinely production-grade in design and fully unit-tested. Biggest gaps: no entity ever flows end-to-end through CRUD, no UI is wired to any API, SharedSchema — the piece every other layer's metadata story depends on — isn't in the build. Biggest risks: CrudEngine cannot compile as committed; Rbac cannot compile as committed; a real-looking database credential is committed in `appsettings.json`. Immediate priorities: fix the two broken builds, give SharedSchema a real `.csproj` and wire it into CrudEngine, and prove one entity end-to-end (API → DB → UI) before adding any further modules.

This document is organized as a reference manual: repository inventory and architecture first, then a capability-by-capability audit of APIPlatform and UIPlatform, then the practical "how do I build on this today" material (integration manual, patterns, cookbook), then quality/readiness and roadmap. Every non-trivial claim cites a file and, where useful, a line number, so any statement here can be re-checked against the repository directly.

---

## 2. Platform Vision

Two documents state intent: the root `README.md` and `API Platform — Master Engineering Context.md`. Neither is treated here as evidence of what exists — only of what was planned.

`README.md` lists 19 "Key Features" — modular architecture, configuration-driven development, multi-database support, JWT auth, a workflow engine, dynamic form engine, generic CRUD engine, enterprise data grid, dashboard framework, notification platform, search framework, SignalR realtime, scheduler, reporting, SAP integration, AI integration, caching, logging/diagnostics, security, and a plugin architecture — pitched at building ERP, CRM, HRMS, inventory, MES, QMS, project management, help desk, LMS, healthcare, school management, finance, and multi-company SaaS applications.

`API Platform — Master Engineering Context.md` is narrower and more precise: it names `APIPlatform.Database` (Dapper, SQL Server + SAP HANA), `APIPlatform.Notification`, `APIPlatform.Realtime`, `APIPlatform.Audit`, and `APIPlatform.CrossCutting` ("orchestration/composition only; no business or feature logic") as the platform's building blocks, under the explicit goal of keeping business logic in applications, not in the platform.

> **Vision vs. actual naming.** The engineering-context document's own module names don't match the repository: it names `APIPlatform.Realtime`, but the actual (empty) folder is `APIPlatform.SignalR`; it names `APIPlatform.Audit` and `APIPlatform.CrossCutting`, and neither folder exists anywhere in the repo, under any name. This is a documentation/planning drift, not an implementation gap — worth resolving before it causes confusion, but not evidence anything was removed.

Every README feature is addressed capability-by-capability in Sections 5–16 (APIPlatform) and 17–23 (UIPlatform). None of the 19 listed features should be assumed present from this list alone.

---

## 3. Repository Inventory

The repository is organized into `backend/` (.NET, net10.0), `frontend/` (React 19 / TypeScript, pnpm-structured), `nucleus/` (shared schema), `docs/`, `tools/`, and `scripts/`. The last three are empty at every level checked. Counts below are exact file counts from the repository at audit time, not estimates.

### Backend — `backend/src/*` (net10.0, C#)

| Project | .cs files | In .sln? | Builds? | Status |
|---|---|---|---|---|
| `APIPlatform.Foundation` | 23 | Yes | ✓ | `FOUNDATION_ONLY` |
| `APIPlatform.Shared` | 7 | Yes | ✓ | `COMPLETE` (DTOs) |
| `APIPlatform.Logging` | 4 | Yes | ✓ | `COMPLETE` |
| `APIPlatform.Configuration` | 2 | Yes | ✓ | `COMPLETE` |
| `APIPlatform.Validation` | 5 | Yes | ✓ | `PARTIAL` |
| `APIPlatform.Database` (`APIPlatform.Data.csproj`) | 23 | Yes | ✓ | `PARTIAL` |
| `APIPlatform.Database.Migration` | 18 | Yes | ✓ | `PARTIAL` |
| `APIPlatform.Notification` | 18 | Yes | ✓ | `PARTIAL` |
| `APIPlatform.Authentication` | 49 | No | ✓ (via Playground) | `PARTIAL` |
| `APIPlatform.Authorization` | 74 (4 csproj) | No | Rbac: ✗ 1 error | `FOUNDATION_ONLY` |
| `APIPlatform.CrudEngine` | 52 | No | ✗ 38 errors | `NOT_IMPLEMENTED` |
| `APIPlatform.AI / Cache / Diagnostics / Integration / Numbering / Reporting / SAP / Scheduler / Search / Security / SignalR / Storage / Sync / Workflow` | 0 each, ×14 | No | n/a | `NOT_IMPLEMENTED` |

### Backend — playground, samples, tests

| Project | Path | Files | Status |
|---|---|---|---|
| `APIPlatform.Playground` | `backend/playground/APIPlatform.Playground` | 18 source files | Builds, runs, exercises 4 modules end-to-end |
| `Sample.CRM.Api / Sample.HRMS.Api / Sample.Inventory.Api / Sample.WebApi` | `backend/samples/*` | 0 | Empty — folder names only |
| `APIPlatform.Database.Tests` | `backend/tests/*` | 4 test files, 18 tests | Pass |
| `APIPlatform.Database.Migration.Tests` | `backend/tests/*` | 11 test files, 38 tests | Pass |
| `APIPlatform.Notification.Tests` | `backend/tests/*` | 7 test files, 40 tests | Pass |
| `APIPlatform.Authentication.Tests / Search.Tests / Sync.Tests / Workflow.Tests` | `backend/tests/*` | 0 | No `.csproj` — placeholder folders |

### Frontend — `frontend/packages/*` (TypeScript, React 19)

| Package | Files | Status |
|---|---|---|
| `ui-platform-foundation` | 24 (20 ts/tsx) | `COMPLETE` infra, `UNCONSUMED` |
| `ui-platform-auth` | 22 (20 ts/tsx) | `PARTIAL` — backend mismatch, `UNCONSUMED` |
| `ui-platform-forms` | 32 (30 ts/tsx) | `COMPLETE` engine, `UNCONSUMED` |
| `ui-platform, calendar, core, crud, dashboard, editor, grid, hooks, layout, notification, routing, search, shared, signalr, storage, theme, utils, workflow` | 0 each, ×17 | `NOT_IMPLEMENTED` |

### Frontend — apps, playground, tests; other roots

| Path | Contents |
|---|---|
| `frontend/apps/sample-crm, sample-hrms, sample-inventory` | Empty |
| `frontend/playground/ui-platform-playground` | Empty |
| `frontend/tests` | Empty |
| `frontend/pnpm-workspace.yaml`, `frontend/package.json` | 0 bytes — workspace not wired at root |
| `nucleus/shared/Nucleus.SharedSchema` | 9 loose .cs files, no .csproj — see §24 |
| `docs/api-platform, architecture, images, release-notes, samples, ui-platform` | Empty |
| `tools/, scripts/, .github/workflows/` | Empty — no CI pipeline configured |
| `backend/build/Directory.Build.props, Directory.Packages.props` | 0 bytes — no central package management despite the file existing |
| `CHANGELOG.md` | 0 bytes |

One consequence worth stating plainly: **every "application," "sample," and "playground" directory in the frontend, and every backend sample, is an empty folder.** The only running, buildable, testable artifact in the entire repository is `APIPlatform.Playground`.

---

## 4. Actual Architecture

The layering that exists in code is narrower than "Application → Platform → Infrastructure → Database," because there is no Application layer running anywhere yet. What's provable from dependency graphs and a passing build is a two-tier shape: **Host (Playground) → Platform modules → ADO.NET providers**.

```text
EnterprisePlatform (as actually wired, not as planned)
│
├── APIPlatform.Playground  (the only host that exists)
│    ├── APIPlatform.Authentication ──► APIPlatform.Foundation
│    ├── APIPlatform.Database (Data.csproj) ──► APIPlatform.Foundation
│    ├── APIPlatform.Database.Migration ──► Database, Foundation
│    ├── APIPlatform.Validation
│    ├── APIPlatform.Configuration
│    ├── APIPlatform.Logging
│    └── APIPlatform.Shared  (referenced by playground; not used by other src/ projects)
│
├── APIPlatform.Notification ──► Database, Foundation   (builds, tested — not referenced by Playground)
├── APIPlatform.Authorization/Rbac ──► its own Foundation.Stub + SharedSchema.Stub  (isolated island — see §12)
├── APIPlatform.CrudEngine ──► Nucleus.SharedSchema (path doesn't exist) + undefined IEntityDefinitionProvider  (does not compile)
└── nucleus/shared/Nucleus.SharedSchema  (loose files, no .csproj, not in any build graph)

frontend/packages/ui-platform-foundation  (no deps)
frontend/packages/ui-platform-auth ──► ui-platform-foundation  (real import — the only real cross-package edge)
frontend/packages/ui-platform-forms       (declares dep on foundation; zero real imports of it)
  — none of the three is imported by any app, playground, or test in the repo —
```

### Expected vs. actual

| Aspect | Expected (README / vision docs) | Actual (code) | Impact |
|---|---|---|---|
| Layering | Application → Platform → Infrastructure → DB, with CrossCutting orchestrating platform modules | Host (Playground) directly composes 8 platform projects; no CrossCutting/orchestration layer exists | Every consumer must hand-wire every module it needs, exactly as Playground does |
| Metadata-driven CRUD | Applications define `EntityDefinition`s, platform renders API + UI from them | CrudEngine consumes the right shape but cannot compile — the schema project it needs has no `.csproj` | No entity, anywhere in the repo, can be exposed through the generic CRUD path today |
| Realtime notification delivery | SignalR pushes notification/workflow events to clients | `APIPlatform.SignalR` is an empty folder; Notification is pull-only | Any "realtime" UX must be built from scratch, or as polling |
| RBAC enforcement | Endpoints protected by permission/role/policy checks | Rbac engine is complete in isolation, but nothing calls it — no `IAuthorizationHandler`, no `AddAuthorization`, anywhere in the repo | The only enforced protection today is plain `[Authorize]` (authenticated or not) — see §12 |

**Recommendation:** treat CrudEngine + SharedSchema as the single highest-leverage fix (§39) — nearly every other planned capability is described in the vision as sitting on top of that pair, and today neither compiles nor is wired in.

---

## 5. APIPlatform Overview

Eleven backend projects contain code; fourteen more exist only as empty folder names matching README feature bullets (AI, Cache, Diagnostics, Integration, Numbering, Reporting, SAP, Scheduler, Search, Security, SignalR, Storage, Sync, Workflow). Of the eleven with code, four are genuinely production-shaped and covered by passing tests (Database, Database.Migration, Notification, and the login/JWT half of Authentication); the rest are either thin-but-real infrastructure (Foundation, Shared, Logging, Configuration, Validation) or islands that don't compile or aren't wired to anything (CrudEngine, Authorization).

Full per-capability status is in the matrix below (§6); request-flow detail is in §7; each major module gets its own deep section in §8–§16.

---

## 6. APIPlatform Capability Matrix

| Capability | Project | Entry point | Config | Tests | Status |
|---|---|---|---|---|---|
| Dapper query/execute/scalar/multi-result | Database | `SqlDatabaseExecutor` | `DatabaseOptions` | 18 | `COMPLETE` |
| SQL Server provider | Database | `SqlServerDatabaseProvider` | `Database:Provider=SqlServer` | ✓ | `COMPLETE` |
| SAP HANA provider | Database | `HanaDatabaseProvider` | `Database:Provider=Hana` | ✓ | `COMPLETE` |
| Stored procedures | Database | `StoredProcedureExecutor` | — | 0 direct | `COMPLETE` |
| Transactions (begin/commit/rollback-on-dispose) | Database | `DatabaseTransaction` | — | partial | `COMPLETE` |
| Retry policy | Database | `IDatabaseRetryPolicy` | RetryCount/RetryDelay (unused) | — | `STUB` (no-op only) |
| Pagination helper (query-level) | Database | — | — | — | `NOT_IMPLEMENTED` |
| Diagnostics listener | Database | `DatabaseDiagnosticsListener` | — | — | `STUB` (0 registered) |
| Schema migration runner | Database.Migration | `MigrationRunner` | reuses DatabaseOptions | 38 | `COMPLETE` (1 schema shipped) |
| Result / error envelopes | Foundation | `Result`, `PagedResult`, `ErrorInfo` | — | — | `COMPLETE` |
| Generic repository contract | Foundation | `IRepository<T>` | — | — | `FOUNDATION_ONLY` |
| ICurrentUser / ITenantContext / IClock | Foundation | interfaces only | — | — | `FOUNDATION_ONLY` |
| Validation pipeline | Validation | `ValidationService` | — | — | `PARTIAL` (0 validators exist) |
| Logging facade | Logging | `IPlatformLogger<T>` | `LoggingOptions` (unread) | — | `COMPLETE` (thin) |
| Options binding helper | Configuration | `BindPlatformOptions<T>` | — | — | `COMPLETE` (thin) |
| Login / JWT / password hash | Authentication | `AuthenticationService`, `JwtService` | `Authentication:Jwt` | 0 (test project has no .csproj) | `COMPLETE` |
| Refresh token | Authentication | `RefreshTokenService` | `Authentication:Jwt` | 0 | `PARTIAL` (dead-ends by design bug) |
| Account lockout | Authentication | `ValidationStage` | — | 0 | `FOUNDATION_ONLY` |
| External auth providers (OAuth/LDAP) | Authentication | `IExternalAuthProvider` | — | 0 | `STUB` |
| RBAC (grants, policy, field mask, row filter) | Authorization/Rbac | `PermissionEvaluator` | — | 0 (console harness only) | `COMPLETE` in isolation / NOT wired |
| ASP.NET Core policy integration | Authorization/Rbac | — | — | — | `NOT_IMPLEMENTED` |
| Generic CRUD (get/list/insert/update/delete) | CrudEngine | `CrudEngine<TEntity>` | — | 0 | `NOT_IMPLEMENTED` (won't compile) |
| Metadata-driven SQL generation | CrudEngine | `QuerySqlBuilder`, `SqlQueryBuilder` | — | 0 | `FOUNDATION_ONLY` (unreachable) |
| Notification create/persist/query | Notification | `NotificationService` | — | 40 | `COMPLETE` |
| Notification delivery (push/email/realtime) | Notification | — | — | — | `NOT_IMPLEMENTED` |
| Search / query engine | Search | — | — | — | `NOT_IMPLEMENTED` (empty folder) |
| SignalR / realtime | SignalR | — | — | — | `NOT_IMPLEMENTED` (empty folder) |
| File / document storage | Storage | — | — | — | `NOT_IMPLEMENTED` (empty folder) |
| Workflow engine | Workflow | — | — | — | `NOT_IMPLEMENTED` (empty folder) |
| Caching, Diagnostics, Scheduler, Numbering, Reporting, SAP, AI, Integration, Sync, Security | 10 more empty folders | — | — | — | `NOT_IMPLEMENTED` |

---

## 7. API Request Lifecycle

Traced against the only real request path in the repository: `POST /api/auth/login` in `APIPlatform.Playground`.

| Stage | Class / file | What happens |
|---|---|---|
| HTTP entry | `AuthenticationController.Login` | `backend/playground/APIPlatform.Playground/Controllers/AuthenticationController.cs:29` |
| Middleware pipeline | `Program.cs` | Swagger → `UseAuthentication()` → `UseCurrentUserContext()` → `UseAuthorization()` → `MapControllers()` |
| 1 — Identity resolution | `IdentityResolutionStage` → `PlaygroundIdentityResolver` | Hardcoded single user (`admin` / `Admin@123`) — no database lookup in the playground demo resolver |
| 2 — Context enrichment | `ContextEnrichmentStage` | Attaches tenant/app context to the pipeline state |
| 3 — Validation | `ValidationStage` | Checks identifier/password presence, active flag, lockout/expiry fields (not enforced by writes — see §11) |
| 4 — Planning | `AuthenticationPlanner` | Always plans `Strategy = Local` — external providers are interface-only |
| 5 — Execution | `AuthenticationExecutor.ExecuteAsync` | `Pbkdf2PasswordHasher.Verify` → `ClaimsBuilder.Build` → `JwtService.Generate` → `SessionService.CreateAsync` → optional `RefreshTokenService.Generate` → `NoOpAuthenticationEventPublisher` |
| 6 — Response mapping | `ResponseMappingStage` | Builds `AuthenticationResponse` DTO (Ok, AccessToken, RefreshToken, ExpiresAt, SessionId, User) |
| Serialization | ASP.NET Core default JSON | No custom response wrapper is applied at the controller level for this endpoint — the DTO is returned directly |
| Client | — | Receives 200 + DTO, or 401 + failure DTO |

**Authenticated request** (e.g. `GET /api/auth/protected`, decorated `[Authorize]`): JWT bearer validation runs via ASP.NET Core's own `AddJwtBearer` (wired by the host, not the Authentication module — `AuthenticationExtensions.cs:27`), issuer/audience/lifetime/signing-key all checked against `JwtOptions`. `CurrentUserContextMiddleware` then populates `ICurrentUserContext` from the validated `ClaimsPrincipal`. **No authorization policy or permission check runs beyond "is this token valid"** — the Rbac engine described in §12 is never consulted on this or any path.

Database-backed requests (e.g. `POST /api/database-migration/run`) follow a parallel, simpler shape: controller → `IMigrationRunner`/`IDatabaseExecutor` → Dapper → SQL Server/HANA → typed result → controller → JSON, with **no authentication/authorization gate on the migration endpoints at all** (flagged again in §32).

---

## 8. Database Platform

`APIPlatform.Database` (assembly `APIPlatform.Data`) is the most mature module in the repository: a provider-agnostic Dapper wrapper hiding all Dapper types behind `IDatabaseExecutor`, with two real ADO.NET providers.

- **SQL Server** — backed by `Microsoft.Data.SqlClient 5.2.2`. `SqlServerDatabaseProvider.CreateConnection` returns a plain `SqlConnection`. (`Providers/SqlServerDatabaseProvider.cs:8`)
- **SAP HANA** — backed by `Sap.Data.Hana.Net.v10.0 2.29.25` (official SAP ADO.NET driver). Same minimal connection-factory shape. (`Providers/HanaDatabaseProvider.cs:13`)
- **Others** — no SQLite, PostgreSQL, MySQL, or Oracle provider exists anywhere in the repo, despite the `DatabaseProvider` enum design allowing more. (`Options/DatabaseProvider.cs:9`)

### Supported access patterns

| Operation | How | Class |
|---|---|---|
| Get / list (raw SQL) | `QueryAsync<T>`, `QueryFirstOrDefaultAsync<T>` | `SqlDatabaseExecutor.cs:51-77` |
| Insert / update / delete | `ExecuteAsync` | `SqlDatabaseExecutor.cs:42` |
| Scalar | `ExecuteScalarAsync<T>` | `SqlDatabaseExecutor.cs:79` |
| Multiple result sets | `QueryMultipleAsync` → `MultiResultReader` | `SqlDatabaseExecutor.cs:88`, `MultiResultReader.cs:11` |
| Stored procedure | `CommandType.StoredProcedure` via any of the above, or `IStoredProcedureExecutor` | `StoredProcedureExecutor.cs:8-29` |
| Parameterized query | `IReadOnlyDictionary<string, object?>` → Dapper `DynamicParameters` | `SqlDatabaseExecutor.cs:166` |
| Transaction | `BeginTransactionAsync` → `IDatabaseTransaction`, auto-rollback on dispose if uncommitted | `SqlDatabaseExecutor.cs:118`, `DatabaseTransaction.cs:13-64` |
| Pagination | *not provided at this layer* | CrudEngine's `QuerySqlBuilder` generates dialect-specific paging SQL, but that module doesn't compile (§9) |

### Configuration

```json
"Database": {
  "Provider": "SqlServer",
  "ConnectionString": "…",
  "CommandTimeoutSeconds": 30,
  "RetryCount": 0,
  "DefaultIsolationLevel": "ReadCommitted"
}
```

`RetryCount`, `RetryDelay`, `DefaultSchema`, and `EnableLogging` are bound but never read by any code path — dead configuration surface (`Options/DatabaseOptions.cs:9-19`). The retry policy in effect is always `NoOpDatabaseRetryPolicy` — genuine retry logic doesn't exist yet (documented in-code as "Not implemented in V1," `IDatabaseRetryPolicy.cs:6`).

> **Documented pattern.** Registration is two calls, always in this order: `services.AddSqlServerProvider()` (or `AddHanaProvider()`) then `services.AddDatabase(options => configuration.GetSection("Database").Bind(options))` — see `backend/playground/.../Extensions/DatabaseExtensions.cs:13-18`. `AddDatabase` alone registers no provider; forgetting the provider call throws at first connection attempt via `DatabaseProviderFactory.GetProvider`.

Test coverage (18 tests, `APIPlatform.Database.Tests`) is unit-level only — configuration binding, DI wiring/lifetimes, provider resolution by kind, and a reflection-based check that no ADO.NET-specific type ever leaks through the public interfaces. No test opens a real connection to SQL Server or HANA, and no test exercises `SqlDatabaseExecutor`'s actual Dapper call paths.

---

## 9. CRUD Engine

> **Does not compile.** `APIPlatform.CrudEngine` fails `dotnet build` with 38 errors, verified directly. Two independent, fatal causes: (1) `APIPlatform.CrudEngine.csproj:12` references `..\Nucleus.SharedSchema\Nucleus.SharedSchema.csproj`, which resolves to `backend/src/Nucleus.SharedSchema/` — a path that has never existed in this repository; (2) `GenericRepository.cs:19,26` and `EntityMetadataCache.cs:10,13` depend on `IEntityDefinitionProvider`, imported from `APIPlatform.Foundation.Interfaces`, which is declared nowhere in the repo.

Despite this, the **design** is coherent and worth documenting as intent: a 6-stage pipeline (`MetadataResolutionStage → ContextEnrichmentStage → ValidationStage → ExecutionPlanningStage → ExecutionStage → ResponseMappingStage`), a single generic `CrudEngine<TEntity>`/`GenericRepository<TEntity>` pair meant to serve every entity without hand-written per-entity repositories, and SQL generated entirely from an `EntityDefinition` passed in from Nucleus.SharedSchema — genuinely, pervasively used throughout (`QuerySqlBuilder.cs`, `SqlQueryBuilder.cs`, `CrudContext.cs` all import `Nucleus.SharedSchema.Models`).

| Piece | Class | Design status |
|---|---|---|
| Public facade | `ICrudEngine<TEntity>` / `CrudEngine<TEntity>` | `Engine/CrudEngine.cs:12-93` — GetAsync, ListAsync, InsertAsync, UpdateAsync, DeleteAsync |
| Data access | `GenericRepository<TEntity>` | `Repositories/GenericRepository.cs:16-112` |
| Metadata cache | `EntityMetadataCache` | `Caching/EntityMetadataCache.cs:8-17` |
| Filtering | `FilterClauseBuilder` | Equality filters only — no range/contains/in yet |
| Sorting / paging | `SortClauseBuilder`, `PagingClauseBuilder` | Dialect-aware — `OFFSET/FETCH` (SQL Server) vs `LIMIT/OFFSET` (HANA) |
| Dialects | `SqlServerDialect`, `HanaDialect` | Present and distinguished |
| Host wiring | `AddCrudEngine()` | Never called outside its own file; no controller of any kind exists in this project or Playground |

Nothing in this module can be exercised end-to-end today — not because the design is wrong, but because it cannot be compiled and nothing hosts it. Fixing the two reference breaks (§39) is a prerequisite for every capability that the vision documents describe as building on generic CRUD.

---

## 10. Query / Search Engine

> **Documented, not implemented.** `backend/src/APIPlatform.Search` exists as a folder with zero files. There is no dynamic filter engine, no search index, no aggregation/grouping/join support, and no security-filtering layer anywhere in the repository outside of CrudEngine's basic (and unreachable) equality filter builder described in §9.

The closest thing to query infrastructure that actually runs is CrudEngine's `QuerySqlBuilder` (filter/sort/page SQL composition) — but since CrudEngine doesn't compile, none of it is usable. There is no supported usage model to document today: an application wanting dynamic filtering, search, or field selection must write its own SQL against `IDatabaseExecutor` directly (Pattern A, §28).

---

## 11. Authentication

A host-agnostic, 6-stage pipeline (`IdentityResolutionStage → ContextEnrichmentStage → ValidationStage → AuthenticationPlanningStage → AuthenticationExecutionStage → ResponseMappingStage`, orchestrated by `AuthenticationPipeline.cs:14-56`). The login/JWT/hashing core is genuinely solid; refresh is implemented but functionally dead-ended.

**What's real:**
- **JWT generation** — HS256, `SymmetricSecurityKey` from config, claims for identity/tenant/company/branch/department/role×N/permission×N. `Jwt/JwtService.cs:12-53`
- **Password hashing** — PBKDF2-SHA512, 310,000 iterations, 16-byte salt, constant-time compare. Not BCrypt/Argon2, but a sound standards-based choice. `Security/Pbkdf2PasswordHasher.cs:9-36`
- **Claims** — built centrally by `Claims/ClaimsBuilder.cs:10-50`, extensible via `IClaimsBuilderExtension`
- **Session** — created per login via `SessionService`, in-memory store

**What's broken or missing:**
- **Refresh is a dead end** — token generation, storage (`InMemoryRefreshTokenStore`), validation, and revocation all work, but `AuthenticationService.RefreshAsync` (lines 37-49) *always* returns `Ok=false, ErrorCode="REAUTH_REQUIRED"`, even for a valid token — by design comment, but the practical effect is `POST /api/auth/refresh` can never issue a usable new access token.
- **Account lockout** — `UserInfo.FailedAttemptCount`/`IsLocked` fields exist and are read by `ValidationStage`, but nothing ever writes them — lockout can never trigger.
- **External auth (OAuth/LDAP)** — `IExternalAuthProvider` is an interface with zero implementations; `AuthenticationPlanner` always plans `Strategy = Local`.
- **Session store is in-memory** — no DB/Redis backing, will not survive a process restart or scale past one instance.

### Login flow, UI to storage

```text
UI (ui-platform-auth, unconsumed)              APIPlatform
LoginForm → useAuth().login()
  → AuthService.login()
    → axios POST {apiBaseUrl}/auth/login  ─────►  AuthenticationController.Login
                                                      → AuthenticationPipeline (6 stages)
                                                      → AuthenticationResponse JSON
  ← applyAuthResponse() ◄────────────────────────  { Ok, AccessToken, RefreshToken, User, … }
  → TokenService.save()   (decodes JWT, stores claims)
  → localStorage / sessionStorage (per authConfig.persistence)
  → authStore (Zustand) set to Authenticated
  → SessionService schedules silent-refresh + hard-expiry timers
```

> **Frontend / backend mismatch.** The frontend's `refreshPath` is `/auth/refresh-token` — the backend route is `api/auth/refresh`. The frontend's refresh request body is `{ refreshToken }` — the backend's `RefreshRequest` requires `{ RefreshToken, UserId }`. The frontend calls `/auth/logout` — **no logout endpoint exists anywhere in the backend**. Only the login call and its response shape line up cleanly. These were never run against each other — the mismatches are self-flagged in the frontend's own `src/types/assumptions.ts:1-9` as an explicit "assumption boundary."

### How a new application integrates authentication

1. Reference `APIPlatform.Authentication` + `APIPlatform.Foundation` from the host project.
2. Call `services.AddAuthenticationPlatform()` (registers the pipeline and in-memory stores) — this does **not** wire ASP.NET Core's own JWT bearer scheme.
3. Separately call `services.AddAuthentication(...).AddJwtBearer(...)` yourself, as Playground does in `AuthenticationExtensions.cs:27-47` — the split is intentional so non-web hosts can reuse the pipeline.
4. Implement `IIdentityResolver` against your real user store — the only implementation shipped (`PlaygroundIdentityResolver`) is a hardcoded demo user and must not be reused.
5. Bind `Authentication:Jwt` (SecretKey, Issuer, Audience, ExpiryMinutes, RefreshTokenExpiryDays) from real, non-committed secrets — see §32 for why the current appsettings value must not be copied.
6. Call `app.UseAuthentication()` then `app.UseCurrentUserContext()` then `app.UseAuthorization()`, in that order.

---

## 12. Authorization

`APIPlatform.Authorization` contains four csproj: `APIPlatform.Rbac` (the real engine, 60+ files), and three support/stub projects. The engine itself is a genuinely complete, self-consistent hybrid **role + fine-grained permission-key + policy** model with field masking and row filtering — evaluated entirely in isolation from the rest of the platform.

> **Currently fails to build.** `dotnet build` on `APIPlatform.Rbac.csproj` fails with 1 error: `Contexts/FieldMaskDescriptor.cs:24` — `CS0120: An object reference is required for the non-static field, method, or property 'FieldMaskDescriptor.FieldAccess'`. This is a genuine code bug, unrelated to CrudEngine's missing-reference problem.

### Model

`Role` grants `PermissionGrant`s (a `PermissionKey` string + `PermissionEffect` Allow/Deny — deny always wins, `Resolution/PermissionResolver.cs:43-54`). On top, named `PolicyRule`s run boolean delegates via `PolicyEngine`, fail-closed — an unregistered policy denies (`Policy/PolicyEngine.cs:17-30`). A 6-stage pipeline (`PermissionResolutionStage → ContextEnrichmentStage → ValidationStage → PlanningStage → ExecutionStage → ResponseMappingStage`) additionally supports field-level masking and row-level filtering, orchestrated by `PermissionEvaluator.cs:14-52`.

> **Foundation only — implementation not found (host wiring).** `[RequirePermission]` (`Attributes/RequirePermissionAttribute.cs:10-20`) is explicitly documented as a marker only — "Rbac deliberately has no ASP.NET Core dependency." There is no `IAuthorizationHandler`, no `IAuthorizationRequirement`, and no `AddAuthorization(...)` call anywhere in the entire repository (repo-wide grep, zero hits). The attribute is defined but applied to nothing. `AddRbac()` is called only from `Nucleus.TestHarness.Rbac/Program.cs`, a manual console smoke test, not from Playground or any other host.

### The stub problem

`APIPlatform.Rbac.csproj` references two local stand-in projects rather than the real ones that already exist in this repository: `APIPlatform.Foundation.Stub` (a narrower, independently-authored `ICurrentUser`/`ITenantContext` — fewer members than the real `APIPlatform.Foundation/Interfaces/ICurrentUser.cs:10-21`) and `Nucleus.SharedSchema.Stub` (its own `EntityMetadata`/`ISharedSchemaProvider`, structurally divergent from the real `nucleus/shared/Nucleus.SharedSchema/Models/EntityDefinition.cs`). Both stubs are code-commented as temporary — but the real Foundation project they were meant to stand in for has existed in this repo the whole time, and the swap has not been made. Rbac cannot be pointed at the real Foundation/SharedSchema without an adapter today, because the shapes don't match.

**Verdict:** `COMPLETE` as an isolated in-memory permission engine — internally consistent, well-modeled. `FOUNDATION_ONLY` for real use — no DB-backed store, no ASP.NET Core policy wiring, no host consumes it, and it currently fails to build. Today, the only authorization enforced on any live endpoint in the repository is plain `[Authorize]` (authenticated-or-not) — no permission, role, or policy check runs anywhere.

---

## 13. SignalR

> **Documented/planned, but implementation not found.** `backend/src/APIPlatform.SignalR` is an empty folder. No hub, no connection/group management, no access-token handling for WebSocket upgrades, and no publish/subscribe surface exists anywhere in the backend. The frontend has a matching empty `ui-platform-signalr` package. There is no realtime capability in this repository today — every "realtime" or "push" claim in the README is aspirational.

Notification delivery (§14) is the one place a realtime channel would plug in; today it is pull-only.

---

## 14. Notifications

`APIPlatform.Notification` is a real, well-tested persistence/read-model library — creation, targeting, and pull-based querying all work against a real `IDatabaseExecutor`. Delivery does not exist.

| Capability | Status | Detail |
|---|---|---|
| Create + validate | `COMPLETE` | `NotificationService.CreateAsync` — `NotificationService.cs:24-45,123-159` |
| Targeting (user / group / all, with exclusions) | `COMPLETE` | `NotificationTargetRule`, `NotificationTargetKind` |
| Transactional persistence | `COMPLETE` | `NotificationRepository.InsertAsync` — rollback-on-dispose, `NotificationRepository.cs:22-62` |
| List / count for recipient | `COMPLETE` | `ListForRecipientAsync`, `CountForRecipientAsync` — `NotificationRepository.cs:64-83` |
| Unread count / mark read | `COMPLETE` | `NotificationUserState` comparison, upsert-with-retry — `NotificationRepository.cs:129-160` |
| Schema migration | `COMPLETE` | Only real migration set in the repo — SQL Server + HANA DDL, §8 |
| SignalR push | `NOT_IMPLEMENTED` | Zero references to Notification anywhere in the (empty) `APIPlatform.SignalR` |
| Email / other channels | `NOT_IMPLEMENTED` | No email sender exists in the repo |
| Templates | `NOT_IMPLEMENTED` | Title/body are supplied as plain strings by the caller |
| Host integration | `NOT_IMPLEMENTED` | `AddNotification()` never called outside its own file; no controller exposes it anywhere |

757 lines of unit tests (40 total) cover validation rules, exact generated SQL per dialect, and transaction/upsert-retry behavior — all against hand-written fakes, no live database.

---

## 15. Storage / Documents

> **Documented/planned, but implementation not found.** `backend/src/APIPlatform.Storage` is an empty folder. No upload/download/delete, no storage-provider abstraction (local/database/external), no versioning, preview, or document-permission model exists anywhere in the repository.

---

## 16. Workflow

> **Documented/planned, but implementation not found.** `backend/src/APIPlatform.Workflow` is an empty folder, and its dedicated test project `APIPlatform.Workflow.Tests` has no `.csproj` either. No template/stage/task/transition/approval model, no execution engine, no designer, and no persistence exists. Nothing in the repository can currently create or run a workflow of any kind.

---

## 17. UIPlatform Overview

20 package folders exist under `frontend/packages/`; 3 contain code. The root `pnpm-workspace.yaml` and `frontend/package.json` are both 0 bytes — the workspace is not wired even for the 3 real packages, and none of the three has ever produced a build (no `dist/`, no `node_modules` anywhere under `frontend/`). Everything documented in §18-23 is **static-analysis-verified, not build-verified** — `npm install` was not run, to avoid writing a lockfile/node_modules into the repository per the audit's no-modification rule; `tsc` was confirmed unavailable in a clean attempt.

What exists is well-built: a real axios/TanStack-Query/Zustand foundation layer, a genuinely complete auth client (login, JWT decode, silent refresh, guards), and a genuinely complete metadata-driven form engine built on React Hook Form + Zod (not Formik, despite that being a common assumption for enterprise React stacks). **None of the three packages is imported by any other file in the repository** — no app, no playground, no test. They are orphaned, unconsumed foundation code.

---

## 18. UI Capability Matrix

| Capability | Package | Component / hook | Status | Consumed by |
|---|---|---|---|---|
| Configured axios client + interceptors | foundation | `createApiClient`, `apiRequest` | `COMPLETE` | ui-platform-auth |
| ApiResponse envelope unwrap | foundation | `unwrapResponse` | `COMPLETE` | ui-platform-auth |
| Generic TanStack Query hooks | foundation | `useApiQuery`, `useApiMutation` | `COMPLETE` (generic) | `UNCONSUMED` |
| Tenant context | foundation | `TenantProvider`, `useTenant` | `PARTIAL` — never fed a real id | `UNCONSUMED` |
| App composition root | foundation | `AppProvider` | `COMPLETE` | `UNCONSUMED` |
| Error boundary | foundation | `AppErrorBoundary` | `COMPLETE` | `UNCONSUMED` |
| Zustand store factory | foundation | `createStore` | `COMPLETE` | ui-platform-auth |
| Login / logout / session | auth | `AuthProvider`, `useAuth`, `AuthService` | `PARTIAL` — see §11 | `UNCONSUMED` |
| Silent refresh + expiry timers | auth | `SessionService` | `COMPLETE` | `UNCONSUMED` |
| 401-retry-after-refresh interceptor | auth | `AuthContext.tsx:128-152` | `COMPLETE` | `UNCONSUMED` |
| Permission / role hooks + guards | auth | `usePermission`, `PermissionGuard`, `RoleGuard` | `COMPLETE` (UX-only, self-documented non-authoritative) | `UNCONSUMED` |
| Route guard | auth | `AuthGuard` | `COMPLETE` | `UNCONSUMED` |
| Metadata-driven form engine | forms | `NucleusFormProvider`, `Form`, `Field` | `COMPLETE` | `UNCONSUMED` |
| Field-component registry (13 built-ins) | forms | `FieldRegistry`, `registerBuiltInFields` | `COMPLETE` | `UNCONSUMED` |
| Zod validation assembly | forms | `ValidationRegistry` | `COMPLETE` core / `STUB` extension points | `UNCONSUMED` |
| SharedSchema field adapter | forms | `resolveFieldType`, `schemaAdapter.ts` | `COMPLETE` — verified 1:1 match to C# model | `UNCONSUMED` |
| Conditional visibility/enable rules | forms | `evaluateCondition` | `COMPLETE` | `UNCONSUMED` |
| Lookup field data fetching | forms | `LookupService`, `useLookup` | `STUB` — no built-in HTTP resolver, throws unless one is registered | `UNCONSUMED` |
| Grid / table | grid | — | `NOT_IMPLEMENTED` | — |
| Routing / feature registry | routing | — | `NOT_IMPLEMENTED` | — |
| Realtime / SignalR client | signalr | — | `NOT_IMPLEMENTED` | — |
| Notification center UI | notification | — | `NOT_IMPLEMENTED` | — |
| Dashboard, calendar, editor, storage, workflow, layout, theme, hooks, utils, shared, crud, core | 12 more packages | — | `NOT_IMPLEMENTED` | — |

---

## 19. Routing

> **Documented/planned, but implementation not found.** `frontend/packages/ui-platform-routing` is empty. The closest thing that exists is `ui-platform-foundation`'s `AppRouterProvider` — a thin, un-configured wrapper around `react-router-dom`'s `BrowserRouter` (`src/providers/RouterProvider.tsx:12`), with no route registry, no feature/permission-based route filtering, no lazy-loading convention, no breadcrumb derivation, and no 404 handling. There is currently no supported way to "add a route" as a platform capability — an application would configure `react-router-dom` directly.

---

## 20. Forms

`ui-platform-forms` is the most complete UI package in the repository: a real, internally-consistent metadata-driven form engine on **React Hook Form + Zod** (not Formik).

| Piece | What it does | File |
|---|---|---|
| Schema adapter | Local, dependency-free mirror of `Nucleus.SharedSchema`'s `FieldDefinition`/`FieldDataType`/`UiInputType` — verified field-for-field against the real C# enums | `src/types/schemaAdapter.ts:13-75` |
| Form/layout builder | `FormBuilder.buildFormConfig` maps schema fields → `FieldConfig[]` + default/custom layout | `src/builders/FormBuilder.ts:18` |
| Form context | Wires `useForm` + `zodResolver` against a dynamically-built Zod schema, real `<form>` element, pub/sub event bus | `src/contexts/FormContext.tsx:24-57` |
| Field registry | 13 built-in field renderers (text, textarea, number, checkbox, switch, radio, select, multiselect, date, file, hidden, lookup, password) | `src/registry/registerBuiltInFields.ts:11-43` |
| Conditional logic | Recursive evaluator for visibility/enable rules — no `eval`/`Function()`, fully data-driven | `src/utils/conditions.ts:3-22` |
| Lookups | Registry pattern, no built-in HTTP resolver — throws until an app registers one | `src/services/LookupService.ts:15-21` |

The package's own `package.json` declares a peer dependency on `ui-platform-foundation`, but static analysis found **zero actual imports** of it anywhere in `src/` — the dependency is declared but dead in code. Generic vs. application-specific: the engine, registry, and adapter are fully generic; any concrete field/lookup an application needs (e.g. an "Employee picker" lookup) is application-specific by design and must be registered by the consuming app.

---

## 21. Grids

> **Documented/planned, but implementation not found.** `frontend/packages/ui-platform-grid` is empty. No column model, sorting, filtering, pagination, selection, export, virtualization, or server/client data-source abstraction exists anywhere in the frontend. There is no data table capability in this platform today.

---

## 22. State Management

The convention that *is* established (in `ui-platform-foundation` and `ui-platform-auth`, the two packages that actually use state) is consistent enough to document as the intended pattern, even though nothing consumes it yet:

| Concern | Tool | Evidence |
|---|---|---|
| Server data (fetch/cache/invalidate) | TanStack Query, via `useApiQuery`/`useApiMutation` | `foundation/src/hooks/useQuery.ts:15`, `useMutation.ts:15` |
| Cross-cutting client state (session, auth) | Zustand, via the shared `createStore` factory | `foundation/src/stores/createStore.ts:7`; `auth/src/stores/authStore.ts:23` |
| Narrow, provider-scoped values (tenant id, form action closures) | React Context | `foundation/src/contexts/TenantContext.tsx:11`; `auth/src/contexts/AuthContext.tsx:51` |
| Local component state | `useState` — no examples of misuse found | — |

Rule of thumb evidenced by the code: **if it came from the network, it's TanStack Query; if multiple unrelated components need to read/write it, it's Zustand; if it's scoped to one provider subtree, it's Context.** This is a real, followable convention — it is simply not yet followed by anything, because nothing consumes these packages.

---

## 23. API Client / UI Data Flow

Traced through the one place a full loop is wired: `ui-platform-auth`'s login, which — on paper — would flow as follows if a page ever mounted `AuthProvider`:

```text
LoginForm (component)
  → useAuth().login(credentials)
    → AuthContext's login()
      → AuthService.login()
        → getApiClient(getAppConfig())        — foundation's memoized axios singleton
        → apiRequest(client, 'POST', loginPath, credentials)
          → axios request → interceptors (tenant header, bearer header) → HTTP
                                                                              ↓
                                                                    (APIPlatform, §7)
                                                                              ↓
        ← unwrapResponse(response)              — throws ApiError on {success:false}
      ← AuthenticationResponse
    ← applyAuthResponse() writes authStore, schedules SessionService timers
  ← useAuth() re-renders consumers via Zustand subscription
```

This loop has never actually run — there is no page in the repository that mounts `AppProvider`/`AuthProvider`. The wiring is correct in isolation (verified by reading both sides), but is **"Implemented in code, but not sufficiently integrated/validated to be considered complete"** as an end-to-end path, and the refresh/logout legs are additionally mismatched against the real backend (§11).

---

## 24. Shared Schema

`nucleus/shared/Nucleus.SharedSchema` holds 6 model classes and 3 enums — a real, coherent metadata vocabulary — but has **no `.csproj` of its own**, is **not listed in `EnterprisePlatform.sln`**, and is not linked into any project via `<Compile Include>`. It exists purely as intent: the namespace (`Nucleus.SharedSchema.Models`) that CrudEngine's source code assumes, without a build artifact behind it.

| Type | Kind | Purpose |
|---|---|---|
| `EntityDefinition` | Model | Name, DisplayName, SourceType, SourceName, SchemaVersion, IsTenantScoped, Fields, Relationships |
| `FieldDefinition` | Model | Per-field metadata — data type, source type, validation rules, UI hints |
| `RelationshipDefinition` | Model | Entity-to-entity relationship description |
| `ValidationRuleDefinition` | Model | Declarative validation rule attached to a field |
| `UiHintDefinition` | Model | Rendering hints (input type, etc.) consumed by UI form generation |
| `PermissionRequirement` | Model | Declares the permission needed to act on an entity/field |
| `FieldDataType, FieldSourceType, UiInputType` | Enums | Closed vocabularies — verified to match the frontend's hand-maintained `schemaAdapter.ts` mirror field-for-field |

**Consumption:** CrudEngine (backend) references it by broken path (§9). `ui-platform-forms`' `schemaAdapter.ts` maintains an independent, verified-accurate TypeScript mirror rather than a package dependency, explicitly to avoid a hard install-time dependency — a reasonable choice, but one that can silently drift from the C# source since there's no shared package or codegen step linking them. `Nucleus.SharedSchema.Stub` (inside Authorization) is a separate, simpler, hand-rolled duplicate, not a reference to this folder. **No compiling code in the repository consumes the real Nucleus.SharedSchema today.**

---

## 25. Configuration

| Layer | Mechanism | Status |
|---|---|---|
| Central NuGet version pinning | `backend/build/Directory.Packages.props` | `NOT_IMPLEMENTED` — 0 bytes, every csproj pins its own versions independently |
| Shared MSBuild properties | `backend/build/Directory.Build.props` | `NOT_IMPLEMENTED` — 0 bytes, no shared build settings applied |
| Per-module options binding | `BindPlatformOptions<T>()` / `IOptions<T>` | `COMPLETE`, consistent convention across all working modules |
| appsettings.json (host-level) | `backend/playground/.../appsettings.json` | `PARTIAL` — present, but contains committed real-looking credentials, see §32 |
| Frontend env config | `configureApp()` / `VITE_API_BASE_URL` | `COMPLETE` mechanism, default value is a placeholder (`/api`) |
| Frontend workspace config | `pnpm-workspace.yaml`, `frontend/package.json` | `NOT_IMPLEMENTED` — both 0 bytes |
| CI configuration | `.github/workflows/` | `NOT_IMPLEMENTED` — empty, no pipeline defined |
| Feature flags | — | `NOT_IMPLEMENTED` — no mechanism found anywhere |

**Precedence, as actually implemented:** for any bound options class, ASP.NET Core's standard chain applies once `IConfiguration` reaches `BindConfiguration`/`Bind` — `appsettings.json` → `appsettings.{Environment}.json` → environment variables → command-line args (Playground's `Program.cs` introduces no custom overrides). Nothing in the platform code changes or documents this chain — it is exactly ASP.NET Core's default.

---

## 26. Playground Validation

`backend/playground/APIPlatform.Playground` is the only host in the repository, and the only place any platform capability has been proven to actually run.

| Capability | Validated? | Evidence |
|---|---|---|
| Foundation, Shared, Logging, Configuration, Validation | Validated | Referenced in `APIPlatform.Playground.csproj`, DI wired in `Program.cs:19-23` |
| Database (SQL Server provider) | Validated | `DatabaseExtensions.cs:15-17`; `DatabaseHealthController`, `DatabaseValidationController` |
| Database.Migration | Validated (manual trigger) | `DatabaseMigrationController.cs:17-45` — POST /run, GET /status |
| Authentication (login, JWT, me, hash, protected) | Validated | `AuthenticationController.cs:13-97` |
| Authentication (refresh) | Reachable, but backend logic dead-ends — §11 | `AuthenticationController.cs:53-62` |
| CrudEngine, Authorization/Rbac, Notification | Not validated — not referenced by Playground at all | Confirmed absent from `APIPlatform.Playground.csproj`'s `ProjectReference` list |
| All 14 empty backend modules + 17 empty frontend packages | Not validated — nothing to validate | — |

Playground follows the platform's own layering convention correctly for what it does use — it composes each module's `AddXxx()` extension rather than reaching into internals, and its own `DatabaseExtensions`/`AuthenticationExtensions` are a legitimate example of host-level composition (exactly the pattern §27 recommends for a real application). It validates a real slice; it does not validate the platform's headline claims (CRUD, workflow, realtime, forms-to-API, grid).

---

## 27. Application Integration Manual

A worked example — "Employee Management System" — using only capabilities verified to exist and build in this audit. Steps that would rely on non-compiling or non-existent modules are marked as such rather than glossed over.

**Step 1–2 — Project structure & platform references**
Do: Create `EmployeeManagement.Api` (ASP.NET Core Web API, net10.0), modeled on `backend/playground/APIPlatform.Playground`. Add `ProjectReference`s to Foundation, Shared, Logging, Configuration, Validation, Database, Database.Migration, Authentication — the same 8 that Playground uses and that are proven to build together.
Avoid: Referencing CrudEngine or Authorization/Rbac yet — neither compiles as committed (§9, §12).

**Step 3–4 — Configure APIPlatform & database**
Do: Mirror `Program.cs`: `AddAPIPlatformFoundation()`, `AddAPIPlatformLogging()`, `AddAPIPlatformConfiguration(configuration)`, `AddAPIPlatformValidation()`, `services.AddSqlServerProvider()` then `AddAPIPlatformDatabase(configuration)`, `AddAPIPlatformDatabaseMigration()`.
Config: Add a real `Database:ConnectionString`/`Provider` section — never reuse the placeholder committed in Playground's `appsettings.json` (§32).

**Step 5 — Configure authentication**
Do: Implement `IIdentityResolver` against your own `Employees`/`Users` table via `IDatabaseExecutor` — do not reuse `PlaygroundIdentityResolver`. Call `AddAuthenticationPlatform()`, then separately wire `AddAuthentication().AddJwtBearer(...)` yourself (§11).
Common error: Forgetting the second call — `AddAuthenticationPlatform()` alone never registers ASP.NET Core's JWT bearer scheme, so `[Authorize]` endpoints will 500, not 401.

**Step 6–8 — Entities, repositories, APIs**
Do today: Define an `Employee` POCO and a hand-written repository against `IDatabaseExecutor` (Pattern A, §28) — parameterized SQL, Dapper mapping, your own controller. This is the only pattern proven to work end-to-end today.
Not yet available: Generic CRUD via `ICrudEngine<Employee>` — blocked until CrudEngine's two build breaks (§9) are fixed and SharedSchema has a real `.csproj` (§24).

**Step 9–13 — UIPlatform, routes, menus, forms, grids**
Do today: Use `ui-platform-foundation`'s `AppProvider` as your app shell and `ui-platform-auth`'s `AuthProvider`/`AuthGuard` for login — both are real, but first fix the refresh-path and add a real logout endpoint (§11) before relying on them past initial login. Use `ui-platform-forms` for the Employee create/edit form — genuinely production-shaped.
Not yet available: Routing conventions, menu registration, and any data grid — all three are empty packages (§19, §21).

**Step 14–19 — Permissions, workflow, notifications, SignalR, documents, dashboards**
Status: Permissions — build your own `[Authorize]` policies for now, Rbac isn't wired to ASP.NET Core (§12). Workflow, SignalR, documents, dashboards — no platform capability exists (§13, §15, §16, and the dashboard package is empty) — build application-specific code, or wait.
Notifications: `APIPlatform.Notification`'s persistence layer is real and usable if you wire `AddNotification()` yourself and write your own controller (it isn't hosted by any existing project) — but delivery is pull-only; there's no push.

**Step 20 — Build and deploy**
Verified commands: `dotnet build backend/EnterprisePlatform.sln` and `dotnet test backend/EnterprisePlatform.sln` both succeed today (§31). There is no CI workflow to model a deploy pipeline on — `.github/workflows/` is empty — and no frontend build has been verified to succeed (no lockfile, no `node_modules`, §17).

---

## 28. Integration Patterns

| Pattern | Description | Exists today? |
|---|---|---|
| **A — Direct API usage** | Application writes its own controller/service, consumes platform infra (Database, Authentication, Validation, Logging) | Yes — this is exactly what Playground does |
| **B — Platform CRUD usage** | Application calls `ICrudEngine<TEntity>` against its own entity | No — CrudEngine doesn't compile |
| **C — Metadata-driven usage** | Application defines an `EntityDefinition`, platform renders/executes from it (API side) | No — SharedSchema has no build artifact for the API side to consume |
| **D — Hybrid** | Platform infra + custom business logic in the same service | Yes — Playground's `PlaygroundValidationService`/`PlaygroundInitializationService` are a working example |
| **E — Custom UI + Platform API** | Application owns its UI, consumes APIPlatform's HTTP surface | Possible today against Authentication/Database endpoints; no example app exercises it |
| **F — Platform UI + Custom API** | Application uses UIPlatform components (forms, auth) against its own API | Components exist and are real (§20), but never demonstrated against a real API — the one demonstrated pairing (ui-platform-auth ↔ Playground) has path/shape mismatches (§11) |

On metadata-driven UI specifically (Pattern C's UI half): `ui-platform-forms`' schema adapter is real and matches the C# model, but nothing on the API side can currently serve an `EntityDefinition` over HTTP for it to consume — the pattern is half-built, from the UI side only.

---

## 29. Developer Cookbook

**Recipe — Add a raw-SQL repository (works today)**
Prerequisites: Host references `APIPlatform.Database`, has called `AddSqlServerProvider()` + `AddDatabase(...)`. Files: Your own `IEmployeeRepository`/`EmployeeRepository`, constructor-injecting `IDatabaseExecutor`. Platform API: `executor.QueryAsync<Employee>(sql, parameters)`, `executor.ExecuteAsync(sql, parameters)`. Common error: Forgetting `AddSqlServerProvider()` before `AddDatabase()` — throws `DatabaseException` at first use, not at startup.

**Recipe — Protect an endpoint (works today, authentication only)**
Platform API: `[Authorize]` on the action, as in `AuthenticationController.Protected()`, line 78. Limitation: This checks "is the JWT valid," nothing more — there is no permission/role check available yet (§12); do not assume `[Authorize(Roles=...)]` integrates with Rbac, because Rbac is never consulted.

**Recipe — Run a database migration (works today)**
Prerequisites: `AddDatabaseMigration()` called; your migration implements `IMigration` for each supported provider, registered in DI. Trigger: Nothing runs automatically — call `IMigrationRunner.RunAsync()` yourself, e.g. from a controller as `DatabaseMigrationController` does. Reference implementation: `Migrations/Notification/NotificationSqlServerMigration.cs` — the only shipped example.

**Recipe — Send a notification (works today, persistence only)**
Prerequisites: Reference `APIPlatform.Notification`, call `AddNotification()` yourself (not called by any existing host). Platform API: `INotificationService.CreateAsync(request)` — requires application, eventType, title, and ≥1 non-exclusion target. Limitation: Recipient must poll `GetNotificationsAsync`/`GetUnreadCountAsync` — there is no push.

**Recipe — Add a dynamic form (works today, frontend only)**
Prerequisites: `ui-platform-forms` + `ui-platform-foundation` installed and built (neither has a verified successful build yet — plan to debug the first `tsc` run). Platform API: `FormService.buildForm(schemaFields, options)` → `<NucleusFormProvider config={...}><Form/></NucleusFormProvider>`. Common error: Using a field `type` with no registered component throws in `Field.tsx:12` — register custom fields via `FieldRegistry` first.

**Recipe — Add a new entity via generic CRUD (blocked)**
Status: Not possible today — fix CrudEngine's two build breaks and give Nucleus.SharedSchema a real `.csproj` first (§9, §24, §39).

**Recipe — Add a workflow, a grid, a route, realtime, or a document upload (blocked)**
Status: Not possible today — the corresponding module is an empty folder (§13, §15, §16, §19, §21).

---

## 30. Business Logic Boundaries

The vision document is explicit: "Keep business/domain logic inside applications. Build common technical capabilities once in the platform and reuse them across applications." The code that exists respects this well — no domain-specific logic (no "Employee," "Invoice," or "IQS"-shaped code) was found inside any `APIPlatform.*`/`ui-platform-*` project.

**Belongs in the platform:**
- Database access, connection/transaction management, migration running
- Authentication mechanics (JWT, hashing, session, refresh)
- Generic permission/role/policy evaluation (once wired)
- Generic CRUD pipeline and metadata-driven SQL generation (once it compiles)
- Cross-cutting UI infra — API client, query cache wrapper, form engine, error boundary

**Belongs in the application:**
- Concrete entities (Employee, Invoice, Ticket) and their fields/relationships
- Identity resolution against a real user store (`PlaygroundIdentityResolver` is explicitly a demo, not a template to ship)
- Business validation rules (no `IValidator<T>` implementation exists in the platform today — that's correct; they belong per-application)
- Concrete lookups/pickers registered into `LookupService`
- Approval logic, industry-specific calculations, anything IQS/CRM/HRMS-specific

### Do not use this way

- **Don't copy `PlaygroundIdentityResolver` into a real app** — it's a hardcoded single user with no database, meant only to demonstrate the pipeline.
- **Don't assume `[Authorize]` gives you permission checks** — it only checks token validity; Rbac is not wired to it anywhere in the repo (§12).
- **Don't build a second local database abstraction** when `IDatabaseExecutor` already exists — the duplicated-envelope drift already visible (Foundation's `PagedResult` vs. Shared's `PagedResponse`, two incompatible `ValidationResult` types) shows what a third would cost.
- **Don't point new code at the Authorization stub projects** (`Foundation.Stub`, `Nucleus.SharedSchema.Stub`) — they are narrower, incompatible stand-ins for real projects that already exist in this repo; use the real `APIPlatform.Foundation` instead.
- **Don't wire a frontend package to the backend without checking the contract first** — `ui-platform-auth`'s refresh/logout mismatch (§11) is exactly the failure mode of skipping this.
- **Don't reuse the committed `appsettings.json` credentials** in any environment beyond a disposable local dev box — see §32.

---

## 31. Testing

Verified directly: `dotnet build backend/EnterprisePlatform.sln` — 0 errors, 123 warnings (all missing-XML-doc-comment, cosmetic). `dotnet test backend/EnterprisePlatform.sln` — **96 of 96 tests pass**, 0 failed, 0 skipped.

| Area | Tests | Depth | Status |
|---|---|---|---|
| `APIPlatform.Database.Tests` | 18 | Unit — config binding, DI wiring/lifetimes, provider resolution, reflection-based ADO.NET-leak guard. No live DB, no Dapper call-path coverage. | Pass |
| `APIPlatform.Database.Migration.Tests` | 38 | Unit against hand-written fakes (no mocking library used anywhere) — runner ordering/idempotency/fail-fast, history race handling, exact SQL-text generation per dialect. | Pass |
| `APIPlatform.Notification.Tests` | 40 | Unit against fakes — validation rules, exact SQL/params sent, transaction + upsert-retry behavior. | Pass |
| `APIPlatform.Authentication.Tests` | 0 | No `.csproj` — placeholder folder only, despite Authentication being the most-exercised module in Playground | Missing |
| `APIPlatform.Search.Tests / Sync.Tests / Workflow.Tests` | 0 | No `.csproj` — match their (empty) source modules | Missing |
| Frontend (unit/component/E2E) | 0 | No test files anywhere under `frontend/`, no vitest/jest config, no eslint config | Missing |
| Integration / cross-module | 0 | Every backend test project uses fakes or in-process `ServiceCollection`s — none opens a real DB connection or runs two modules together | Missing |

Character worth naming explicitly: this is a strong, disciplined *unit*-test culture (no mocking library, hand-written fakes, exact-SQL-text assertions) wherever it exists — but it exists only for the three modules that also happen to be the most complete (Database, Database.Migration, Notification). Authentication — arguably the highest-risk module given the refresh-token bug (§11) — has zero automated tests.

---

## 32. Security

> **Finding — credentials committed to source control.** `backend/playground/APIPlatform.Playground/appsettings.json` (around line 13-16) contains a SQL Server connection string with a `sa`-account username/password pair, and a separate SAP HANA connection string with a `SYSTEM`-account username/password pair — neither is a placeholder like `YOUR_PASSWORD_HERE`, both read as real, working-looking credentials. Values are intentionally not reproduced in this manual. Whether or not these currently point at a reachable server, committing them sets a bad precedent for every application built on this platform; they should be rotated and moved to user-secrets/environment variables/a vault before this file is used as a template.

| Area | Finding |
|---|---|
| Password storage | PBKDF2-SHA512, 310k iterations, salted, constant-time compare — sound (§11) |
| JWT signing | HS256 with a plain config-string secret, no rotation/KeyVault integration; acceptable for dev, not for production as-is |
| Refresh token | Cryptographically random (`RandomNumberGenerator`), 64 bytes — generation is sound; the broken hand-off (§11) is a functional bug, not a security hole |
| Authorization enforcement | None beyond token validity anywhere in the repo — no endpoint enforces a permission or role today (§12) |
| SQL injection surface | All traced query paths use parameterized Dapper calls (`DynamicParameters`) — no string-concatenated SQL found in Database, Database.Migration, or Notification |
| Unauthenticated endpoints | `DatabaseMigrationController`'s `POST /run` has no `[Authorize]` at all — anyone who can reach the playground can trigger a schema migration |
| Secrets in logs | Not directly assessed — `IPlatformLogger<T>` passes messages through verbatim with no redaction layer; callers are responsible |

---

## 33. Performance

No load testing, benchmarking, or profiling artifacts exist anywhere in the repository — nothing here is measured, so nothing is claimed as fast or slow. What's structurally notable: `IDatabaseConnectionFactory`/`IDatabaseExecutor` are registered `Scoped` (verified by a dedicated regression test, §31), correctly avoiding a shared/singleton connection; the retry policy is a no-op (§8), so transient failures under load are not currently absorbed by the platform; and CrudEngine's designed pagination (dialect-specific `OFFSET/FETCH` vs `LIMIT/OFFSET`) is sound in principle but unreachable until it compiles.

---

## 34. Provider Portability

SQL Server ↔ SAP HANA portability is the one cross-cutting design goal that's been genuinely, consistently honored everywhere it applies:

- `IDatabaseExecutor`/`IDatabaseProvider` abstract both engines behind one interface (§8), and a reflection test enforces that no ADO.NET-specific type leaks through the public surface.
- Migration DDL is hand-written per dialect (bracket vs. double-quote identifiers, `DATETIME2(3)` vs. `TIMESTAMP`, transactional vs. auto-commit DDL) rather than generated from one source — verified correct for the one shipped schema (Notification), tested exhaustively (§31).
- CrudEngine's designed `SqlServerDialect`/`HanaDialect` pair extends the same discipline to paging syntax — unreachable today, but consistent with the pattern.

No third provider (PostgreSQL, MySQL, SQLite, Oracle) exists anywhere, despite the enum-based provider design making one straightforward to add.

---

## 35. Production Readiness

| Capability | Readiness | Why |
|---|---|---|
| Dapper database layer | Near Production Ready | Solid design, tested, both providers real — needs a real retry policy and pagination before "production ready" |
| Schema migration | Near Production Ready | Runner/history/dialect logic is genuinely solid — only one schema shipped to prove it at scale |
| Login / JWT / hashing | Development Ready | Works end-to-end; needs key-rotation story and a real identity resolver before production |
| Refresh tokens | Prototype | Cannot issue a working refresh in its current state (§11) — a functional blocker, not a polish item |
| Authorization/Rbac | Foundation Only | Sound design, but doesn't compile and isn't wired to anything |
| Notification (persistence) | Development Ready | Real and tested; needs a host and a delivery channel to be useful to an app |
| CrudEngine | Not Implemented | Does not compile |
| UI packages (auth, forms, foundation) | Prototype | Well-built in isolation, never run, never built, one real endpoint mismatch found on inspection |
| SignalR, Storage, Workflow, Search, Grid, Routing, and 15 other empty modules | Not Implemented | Zero code |

---

## 36. Remaining Work

**Critical — blocks calling the platform usable**

| Item | Affected | Blocking? |
|---|---|---|
| Fix CrudEngine's two build breaks (dangling ref, undefined interface) | CrudEngine | Yes — blocks Patterns B & C entirely |
| Give Nucleus.SharedSchema a real `.csproj`, add to solution | Nucleus.SharedSchema, CrudEngine | Yes — same blocker as above |
| Fix Rbac's `FieldMaskDescriptor` static/instance bug | Authorization/Rbac | Yes — module doesn't compile at all otherwise |
| Fix or remove the refresh-token dead end | Authentication | Yes for any app needing session longevity |
| Reconcile ui-platform-auth's refresh/logout contract with the backend | ui-platform-auth, Authentication | Yes for any real login flow |

**High — required for enterprise maturity**
- Wire Rbac to ASP.NET Core's policy system and to at least one host
- Wire Notification's `AddNotification()` and a controller into a host
- Add a DB-backed session/refresh-token store (current stores are in-memory)
- Add a real retry policy (current one is a documented no-op)
- Rotate the credentials committed in `appsettings.json`, move to secrets
- Stand up CI (currently zero workflows)

**Medium — developer experience**
- Central package version management (`Directory.Packages.props` is empty)
- Resolve the duplicate `ValidationResult`/`PagedResult`/`PagedResponse` types between Foundation and Shared
- Reconcile the Authorization stub projects with real Foundation/SharedSchema
- Wire the pnpm workspace at the root so the 3 frontend packages can actually be installed/built together

**Low**
- XML doc comments (123 build warnings, all cosmetic)
- Dead config fields (`DefaultSchema`, `EnableLogging` on `DatabaseOptions`)

**Long-term**
- Build the 14 empty backend modules and 17 empty frontend packages against real application demand, not speculatively
- SignalR-based notification/workflow push once Storage/Workflow/SignalR exist

---

## 37. Missing Capabilities

| Gap | Kind |
|---|---|
| Search/query engine, SignalR, Storage, Workflow, Scheduler, Reporting, SAP, AI, Caching, Diagnostics, Numbering, Integration, Sync, Security (backend); Grid, Routing, and 15 more (frontend) | Intentional future scope — folders exist as placeholders, consistent with README's roadmap framing, not a regression |
| CrudEngine not compiling | Missing implementation / integration gap — the design exists, the wiring doesn't |
| Nucleus.SharedSchema without a `.csproj` | Architectural gap — a real dependency (CrudEngine) needs this as a build artifact, not just a folder |
| Rbac not wired to ASP.NET Core | Integration gap — the engine is complete, the plumbing to use it is absent |
| ui-platform-auth/backend contract mismatch | Integration gap — both sides exist, were never run together |
| Vision doc naming (`APIPlatform.Realtime`, `APIPlatform.Audit`, `APIPlatform.CrossCutting`) not matching any folder | Documentation gap |
| Zero frontend tests, zero eslint config | Testing gap |
| Zero CI workflows | Integration/process gap |

---

## 38. Technical Debt

- **Two non-compiling projects committed to the tree** (CrudEngine, Rbac) — either should be marked experimental/excluded from CI expectations, or fixed; leaving them silently broken risks a future contributor assuming they work.
- **Duplicate, incompatible envelope types** — `Foundation.Results.ValidationResult` (immutable record) vs. `Validation.Results.ValidationResult` (mutable class); `Foundation.Results.PagedResult<T>` vs. `Shared.Pagination.PagedResponse<T>` — same namespace-adjacent short names, different shapes, real collision risk for any consumer referencing both.
- **Stale code comments** — the Authorization stub projects are commented as placeholders for "the real APIPlatform.Foundation package (frozen, not part of this codebase yet)," but that real package has existed in this repository the entire time.
- **Dead configuration surface** — `DatabaseOptions.DefaultSchema`/`EnableLogging`, `LoggingOptions.EnableConsoleLogging`/`IncludeSensitiveData` are bound but never read by any code path.
- **Package version skew** — production Database/Database.Migration projects target `Microsoft.Extensions.*` 9.0.0 while their own test projects reference 10.0.11 (compatible under net10.0 today, but drift-prone without central version management).
- **Unused declared dependencies** — `ui-platform-forms` declares `zustand` and a peer dependency on `ui-platform-foundation`, importing neither.

---

## 39. Recommended Next Phase

In dependency order, not effort order — each item unblocks the next:

1. Fix Rbac's one-line build error and CrudEngine's two reference breaks — both are small, mechanical fixes relative to their blast radius.
2. Give `nucleus/shared/Nucleus.SharedSchema` a real `.csproj`, add it to the solution, and point CrudEngine at it correctly.
3. Prove one entity end-to-end through generic CRUD (API only) — this is the first time the platform's central metadata-driven promise would actually be demonstrated.
4. Fix the Authentication refresh dead-end and reconcile `ui-platform-auth`'s contract with the real backend, then mount `AppProvider`/`AuthProvider` in a real (even minimal) page for the first time.
5. Wire Rbac into the host pipeline (an `IAuthorizationHandler` reading its `PermissionEvaluator`) and protect the CRUD endpoints from step 3 with it.
6. Only then begin new empty modules (Search, Grid, Routing, SignalR) — each is described in the vision as sitting on top of the CRUD + Rbac spine this phase establishes.

---

## 40. 10-Year Evolution

Speculative, scoped strictly to what the existing vision documents already claim as direction — not new architecture invented for this manual.

- **Near term** (this repo's own stated next step): Nucleus Builder — a metadata authoring tool sitting on top of SharedSchema + CrudEngine, per the folder name `nucleus/` already reserved for it.
- **Mid term:** the README's remaining feature list — search, realtime, scheduler, reporting, SAP integration, AI integration — each as a thin module following the same "contracts in Foundation, implementation in its own project, opt-in DI extension" pattern already established by Database/Notification.
- **Long term:** multiple generated applications (ERP, CRM, HRMS, etc., per the README's list) sharing one platform core, with CrossCutting (named in the engineering-context doc but not yet built) as the composition layer that lets an application opt into platform capabilities without hand-wiring 8+ `AddXxx()` calls the way Playground currently does.

---

## 41. Complete Capability Map

```text
EnterprisePlatform
│
├── APIPlatform
│    ├── Foundation ................. FOUNDATION_ONLY — contracts, no impls
│    ├── Shared ..................... COMPLETE (DTOs)
│    ├── Logging .................... COMPLETE (thin)
│    ├── Configuration .............. COMPLETE (thin)
│    ├── Validation ................. PARTIAL — pipeline works, 0 validators
│    ├── Database ................... PARTIAL — Dapper + SqlServer + HANA, no retry/paging
│    ├── Database.Migration ......... PARTIAL — solid runner, 1 schema shipped
│    ├── Authentication ............. PARTIAL — login/JWT complete, refresh dead-ends
│    ├── Authorization (Rbac) ....... FOUNDATION_ONLY — complete in isolation, won't build, not wired
│    ├── CrudEngine ................. NOT_IMPLEMENTED — won't compile
│    ├── Notification ............... PARTIAL — persistence complete, no delivery, no host
│    └── AI, Cache, Diagnostics, Integration, Numbering,
│        Reporting, SAP, Scheduler, Search, Security,
│        SignalR, Storage, Sync, Workflow ....... NOT_IMPLEMENTED — empty ×14
│
├── UIPlatform
│    ├── foundation .................. COMPLETE infra, UNCONSUMED
│    ├── auth ......................... PARTIAL — real logic, backend mismatch, UNCONSUMED
│    ├── forms ........................ COMPLETE engine, UNCONSUMED
│    └── calendar, core, crud, dashboard, editor, grid,
│        hooks, layout, notification, routing, search,
│        shared, signalr, storage, theme, utils,
│        workflow, ui-platform (root) ........... NOT_IMPLEMENTED — empty ×17
│
├── SharedSchema ..................... 6 models + 3 enums, no .csproj, ORPHANED from build
│
├── Playground ....................... COMPLETE for its 8-project scope — only running host
│
└── Nucleus Builder ................... NOT_IMPLEMENTED — folder reserved, no code yet
```

---

## 42. File / Code Reference

The subsystems a new developer most needs to understand first, with their real entry points.

| Subsystem | Entry point | Path |
|---|---|---|
| Host composition | `Program.cs` | `backend/playground/APIPlatform.Playground/Program.cs` |
| Database execution | `SqlDatabaseExecutor` | `backend/src/APIPlatform.Database/Execution/SqlDatabaseExecutor.cs` |
| Database DI | `ServiceCollectionExtensions.AddDatabase` | `backend/src/APIPlatform.Database/DependencyInjection/ServiceCollectionExtensions.cs:19` |
| Migration runner | `MigrationRunner` | `backend/src/APIPlatform.Database.Migration/Services/MigrationRunner.cs` |
| Login pipeline | `AuthenticationPipeline` | `backend/src/APIPlatform.Authentication/Pipeline/AuthenticationPipeline.cs:14` |
| JWT generation | `JwtService` | `backend/src/APIPlatform.Authentication/Jwt/JwtService.cs:12` |
| RBAC evaluation | `PermissionEvaluator` | `backend/src/APIPlatform.Authorization/APIPlatform.Rbac/Services/PermissionEvaluator.cs:14` |
| Generic CRUD facade (unreachable) | `CrudEngine<TEntity>` | `backend/src/APIPlatform.CrudEngine/Engine/CrudEngine.cs:12` |
| Notification persistence | `NotificationRepository` | `backend/src/APIPlatform.Notification/Repositories/NotificationRepository.cs` |
| Shared metadata models | `EntityDefinition`, `FieldDefinition` | `nucleus/shared/Nucleus.SharedSchema/Models/` |
| UI API client | `apiClient.ts` | `frontend/packages/ui-platform-foundation/src/api/apiClient.ts` |
| UI auth service | `AuthService.ts` | `frontend/packages/ui-platform-auth/src/services/AuthService.ts` |
| UI form engine | `FormContext.tsx` | `frontend/packages/ui-platform-forms/src/contexts/FormContext.tsx:24` |

---

## 43. Final Platform Status

EnterprisePlatform is at the **Foundation** stage of the classification below — not Developer Preview, because even its best-built corner has never been exercised by a real application, only by its own playground.

| Stage | Meets it? |
|---|---|
| Foundation — core abstractions exist, some modules work in isolation | Yes — this is where the platform sits today |
| Developer Preview — a developer could plausibly build a small real feature against it | Partially — true for Pattern A (§28) against Database/Authentication; not true for CRUD, UI, permissions |
| Application Ready — a full CRUD app with auth and permissions can be built without patching the platform | No — CrudEngine and Rbac wiring block this |
| Enterprise Ready / Platform Ready / Product Ready | No |

Answering the question this audit was scoped to answer: **what can be built today** is a real, database-backed, JWT-authenticated ASP.NET Core API following the Playground's own pattern (Pattern A/D) — solid, tested, honest work. **What cannot yet be built** is anything relying on generic CRUD, permission enforcement, search, realtime, workflow, storage, or a connected UI — each is either non-compiling, unwired, or an empty folder. The gap between the two is well-defined and, per §39, addressable in a specific, ordered sequence rather than a rewrite.

---

*Compiled from direct repository inspection — `dotnet build`, `dotnet test`, and read-only source review across backend and frontend. No source files were modified in the course of this audit. Line numbers reflect the audited commit; re-verify before relying on them after further changes.*
