# Phase 2 — One Generic Entity End-to-End: Implementation Report

## Final Verification

```text
Backend Build:   PASS  (dotnet build backend/EnterprisePlatform.sln — 0 errors)
Backend Tests:   PASS  (162/162 — 141 pre-existing + 21 new; 0 failed, 0 skipped)
Frontend Build:  PASS  (pnpm -r build across ui-platform-foundation/-auth/-forms; tsc --noEmit
                  && vite build for the new test app)
Frontend Tests:  N/A   (no test runner existed in any UIPlatform package before or after this
                  phase; verification was build + typecheck + live browser drive, see Section K)
Runtime:         PASS  (both curl-driven and real-browser-driven, see Section K)
Database:        PASS  (real local SQL Server — Data Source=ANARA, Initial Catalog=IQS_DB —
                  confirmed running on this machine; Employees table created via the platform's
                  own migration engine, full CRUD proven against it)
```

**Answer to the phase's one question — "Can EnterprisePlatform actually build and run one
generic enterprise entity end-to-end using its own platform abstractions?" — is YES**, with
evidence in every section below, and two documented, unfixed, out-of-scope limitations
(Section N).

---

## A. Test Application

```text
Location:     backend/playground/APIPlatform.Playground (extended, not replaced)
              frontend/playground/ui-platform-playground (new)
Purpose:      Prove SharedSchema -> CrudEngine -> Database -> API -> RBAC -> UIPlatform Forms
              for one entity (Employee), per phase2.md's mandate.
Dependencies: Backend: adds project references to APIPlatform.CrudEngine, Nucleus.SharedSchema,
              and APIPlatform.Rbac to APIPlatform.Playground.csproj (previously referenced
              neither — confirmed by inspection before any change was made).
              Frontend: new Vite/React app depending on @nucleus/uiplatform-foundation,
              @nucleus/uiplatform-auth, @nucleus/uiplatform-forms via pnpm workspace:*.
```

Playground was chosen over a new dedicated host per phase2.md's explicit preference. Its
pre-existing CRUD demo (`DatabaseValidationController`/`PlaygroundValidationService`) is
untouched — it hand-writes Dapper calls against `PlaygroundRecord` and is explicitly commented
"must never be used for business logic"; Employee is a fully separate, CrudEngine-driven path.

**Also discovered and fixed as a prerequisite**: `APIPlatform.Playground` and
`APIPlatform.Authentication` were not actually part of `backend/EnterprisePlatform.sln` — only
of a separate, playground-local `.slnx`. The "Full Solution BUILD PASS" claims in
`prompt/phase1.md`/`prompt/phase2.md`'s headers therefore never built Playground at all. Both
projects (plus the new `APIPlatform.Playground.Tests`) are now added to the real solution file,
so `dotnet build backend/EnterprisePlatform.sln` finally covers them.

---

## B. Employee Entity

```text
Entity: Employee (backend/playground/APIPlatform.Playground/Models/Employee.cs)
Fields: Id (Guid, PK) · EmployeeCode · Name · Email · Department (nullable) ·
        IsActive · CreatedOn · ModifiedOn (nullable)
```

```sql
CREATE TABLE [Employees] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [EmployeeCode] NVARCHAR(20)      NOT NULL,
    [Name]         NVARCHAR(200)     NOT NULL,
    [Email]        NVARCHAR(256)     NOT NULL,
    [Department]   NVARCHAR(100)     NULL,
    [IsActive]     BIT               NOT NULL,
    [CreatedOn]    DATETIMEOFFSET    NOT NULL,
    [ModifiedOn]   DATETIMEOFFSET    NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
)
```
No `IDENTITY`/`NEWID()`/`GETDATE()` — matches the platform's documented convention (verified
against `NotificationSchemaSql` and `PlaygroundSqlScripts` before writing this). `Id` is set by
`EmployeesController` (`Guid.NewGuid()` if empty, mirroring `DatabaseValidationController`'s
existing pattern); `CreatedOn`/`ModifiedOn` are set generically by CrudEngine's
`IEntityDefaultValueProvider` (`UtcNowOnCreate`/`UtcNowOnUpdate`), never hardcoded in the
controller.

Metadata: `backend/playground/APIPlatform.Playground/Metadata/EmployeeEntityDefinitionProvider.cs`
— `Name`/`Email` marked `Validation.Required = true` (proves Section H's validation).

---

## C. SharedSchema Integration

`Nucleus.SharedSchema` was not modified. Its real, frozen models
(`EntityDefinition`/`FieldDefinition`/`ValidationRuleDefinition`/`UiHintDefinition`/
`PermissionRequirement`) are used exactly as shipped. Metadata flow:

```text
EmployeesController
  -> ICrudEngine<Employee>              (APIPlatform.CrudEngine.Engine)
    -> CrudContext<Employee>.EntityName = "Employee"
    -> MetadataResolutionStage<Employee>
      -> IEntityMetadataCache.GetOrAdd("Employee", ...)
        -> EmployeeEntityDefinitionProvider.GetDefinition("Employee")   <- application code
          -> Nucleus.SharedSchema.Models.EntityDefinition                <- platform model, unmodified
```

---

## D. CrudEngine Integration

```text
IEntityDefinitionProvider:  EmployeeEntityDefinitionProvider (application-level, registered
                             via services.AddSingleton<IEntityDefinitionProvider,
                             EmployeeEntityDefinitionProvider>() before AddCrudEngine() — the
                             platform intentionally registers no NoOp fallback; confirmed by
                             reading APIPlatform.CrudEngine's own DI extension before writing
                             any code).
CrudEngine<TEntity>:        Unmodified. Six-stage pipeline (Metadata -> ContextEnrichment ->
                             Validation -> [hooks] -> ExecutionPlanning -> Execution ->
                             [hooks] -> ResponseMapping) runs exactly as shipped for Employee.
GenericRepository:          Unmodified. Builds all SQL (SELECT/INSERT/UPDATE/DELETE) purely
                             from EntityDefinition — no Employee-specific SQL exists anywhere
                             (verified: see Section L's grep check).
SQL generation:              List with filters/sort/paging routes through QuerySqlBuilder
                             (proven in D below and Section K's unit tests: EmployeeCode
                             equality filter, Name ORDER BY, SQL Server OFFSET/FETCH paging);
                             plain List/Get/Insert/Update/Delete route through
                             GenericRepository's metadata-driven SqlQueryBuilder.
```

---

## E. Database Runtime

Real SQL Server (`Data Source=ANARA; Initial Catalog=IQS_DB`, the same instance already
configured in `appsettings.Development.json` and confirmed running on this machine —
`MSSQLSERVER` Windows service, `Running`). The `Employees` table is created by
`EmployeeSqlServerMigration` (`backend/playground/APIPlatform.Playground/Migrations/`), an
`IMigration` implementation living in Playground (not in the platform's
`APIPlatform.Database.Migration` assembly), applied through the platform's real
`IMigrationRunner` — history-tracked and idempotent, run automatically at Playground startup
alongside RBAC seeding (`EmployeeModuleInitializationService`).

---

## F. API

```text
GET    /api/employees/{id}   -> ICrudAuthorizationService "read"   -> ICrudEngine.GetAsync
GET    /api/employees        -> ICrudAuthorizationService "read"   -> ICrudEngine.ListAsync
                                 (employeeCode filter, sort, page/pageSize query params)
POST   /api/employees        -> ICrudAuthorizationService "create" -> ICrudEngine.InsertAsync
PUT    /api/employees/{id}   -> ICrudAuthorizationService "update" -> ICrudEngine.UpdateAsync
DELETE /api/employees/{id}   -> ICrudAuthorizationService "delete" -> ICrudEngine.DeleteAsync
POST   /api/auth/logout      -> new; ui-platform-auth always calls one, none existed before
```
All Employee/Auth responses are wrapped in a small app-level envelope (`ApiEnvelope<T>`,
`{success, data, error, traceId}`) matching what `ui-platform-foundation`'s `apiRequest()`
requires — not a platform (`APIPlatform.Shared`) change.

---

## G. Authentication

Uses `APIPlatform.Authentication` unmodified. `PlaygroundIdentityResolver` is now explicitly
labeled TEST ONLY in its doc comments (phase2.md 24's requirement) and recognizes two hardcoded
logins: `admin`/`Admin@123` (full Employee CRUD) and `viewer`/`Viewer@123` (read-only, proves
RBAC deny). Flow: `POST /api/auth/login` -> JWT with `NameIdentifier`/`sub` claims ->
`CurrentUserContextMiddleware` populates `ICurrentUserContext` per request ->
**`HttpCurrentUserContextAdapter`** (new; `Infrastructure/`) bridges that to Foundation's
`ICurrentUser`/`ITenantContext` -> consumed by both `CrudEngine<Employee>` and Rbac's
`DefaultAuthorizationContextFactory`. This adapter closes a real, previously-undiscovered
wiring gap: **nothing in the entire repository implemented `ICurrentUser`/`ITenantContext`
before this phase** — confirmed by a repo-wide search before writing the adapter.

---

## H. RBAC

```text
Permission keys: employee.read / employee.create / employee.update / employee.delete
                 (PermissionKeyBuilder's "{ResourceKey}.{Action}" shape, lowercase to match
                 phase2.md's own example)
User            Role             Allowed                  Denied
admin           employee-admin   read, create, update,     —
                                 delete
viewer          employee-viewer  read                       create, update, delete
```
Proven three ways: (1) unit tests (`EmployeeRbacTests`, 8 cases) directly against
`ICrudAuthorizationService` with a seeded `InMemoryRoleStore`; (2) live `curl` against the
running API — `viewer` POST/DELETE both returned `403 {"code":"forbidden", ...}`, unauthenticated
GET returned `401`; (3) the same 403 path is reachable from the browser UI (create/edit call the
same authorized endpoints). No ASP.NET Core policy/handler plumbing was added —
`ICrudAuthorizationService.AuthorizeAsync` is called directly from `EmployeesController`, exactly
as phase2.md 22 anticipated ("determine whether the existing RBAC API can be called directly").

Per phase2.md 23, the `Nucleus.SharedSchema.Stub` field-permission mismatch inside
`APIPlatform.Rbac` was **not** touched — Employee authorization is resource/action-level only.

---

## I. UIPlatform

```text
Foundation: AppProvider, getApiClient/apiRequest (envelope-aware), useApiQuery/useApiMutation —
            all used as-is; no axios config duplicated in the app.
Auth:       AuthProvider, useAuth, AuthGuard, LoginForm, LogoutButton — all used as-is.
Forms:      FormService.buildForm() + <Field> (registry-backed, RHF+Zod-validated) — real field
            registry and validation mechanism, not a hand-rolled form.
API client: Foundation's shared axios instance, token attached via getAccessToken ->
            config.getAuthToken (the documented wiring pattern).
State/query: TanStack Query (useApiQuery/useApiMutation) + Zustand (authStore, internal to
            ui-platform-auth) — no separate state management added.
```

---

## J. End-to-End Flow

```text
Browser (React)
  -> LoginForm (ui-platform-auth)
    -> AuthService.login -> POST /api/auth/login
      -> AuthenticationController -> IAuthenticationService -> JWT issued
  -> AuthGuard admits -> EmployeeListPage
    -> useEmployeeList -> GET /api/employees
      -> EmployeesController -> ICrudAuthorizationService("read") -> ICrudEngine.ListAsync
        -> SharedSchema metadata -> GenericRepository/QuerySqlBuilder -> Dapper -> SQL Server
  -> EmployeeForm (ui-platform-forms) -> Save
    -> POST /api/employees -> ... -> CrudEngine.InsertAsync -> SQL Server INSERT
  -> Edit -> PUT /api/employees/{id} -> ... -> CrudEngine.UpdateAsync -> SQL Server UPDATE
  -> Delete -> DELETE /api/employees/{id} -> ... -> CrudEngine.DeleteAsync -> SQL Server DELETE
  -> TanStack Query cache invalidated -> list re-renders
```
Every arrow above was independently exercised — see Section K.

---

## K. Tests

### Unit (`backend/playground/APIPlatform.Playground.Tests/Unit/`, 20 tests, fake `IDatabaseExecutor`)
`EmployeeCrudEngineTests` (11): Insert success + generated SQL, `UtcNowOnCreate` applied,
missing-Name/-Email validation failures, Get-by-key, List (plain SelectAll vs.
filtered/sorted/paged via QuerySqlBuilder — asserts `EmployeeCode = @Filter_EmployeeCode`,
`ORDER BY Name`, `OFFSET .. FETCH NEXT ..`), Update (applies `UtcNowOnUpdate`, leaves `CreatedOn`
alone, excludes the PK from the `SET` clause), Delete. `EmployeeRbacTests` (9): admin allowed for
all four actions, viewer allowed only for read, viewer denied for write actions, an unknown user
denied by default (`RbacOptions.DefaultDeny`).
Proves orchestration only — see phase2.md 33's explicit warning, which is why the next test
class exists.

### Integration (`Integration/EmployeeSqlServerIntegrationTests.cs`, 1 test, **real SQL Server**)
Full create -> read -> update -> delete cycle through the actual
`CrudEngine -> GenericRepository -> Dapper -> SQL Server` chain, ensuring the table exists via
the real `IMigrationRunner` first. Cleans up its own row. **Passed.**

### Manual (runtime, both proven — see transcripts retained during this session)
- `curl` against `dotnet run`: login (admin, viewer), full CRUD, `EmployeeCode` filter, `-Name`
  sort + paging, invalid update rejected (`400`, "Name is required."), RBAC 403 for viewer
  writes, `401` unauthenticated, successful delete, `404` after delete, logout.
- Real headless-browser drive (Playwright/Chromium) against the actual Vite dev server +
  running API: login page renders (real `LoginForm`) -> sign in as admin -> Employee list
  renders (empty, then populated) -> "New Employee" opens the real metadata-driven form
  (required-field markers visible) -> Save -> row appears in the list -> Edit -> Department
  updated in the list -> Delete -> row removed. **Zero browser console errors** at any step.

### Not tested
- SAP HANA runtime (phase2.md 35 explicitly does not require this; CrudEngine's existing
  `HanaDialect` unit tests, unrelated to Employee, remain the only HANA-dialect coverage).
- Frontend unit/component tests — none exist for any UIPlatform package (`ui-platform-foundation`
  /`-auth`/`-forms`), before or after this phase; verification here relied on build + typecheck +
  live browser drive instead.
- A genuinely successful token refresh (impossible without the platform change described in
  Section N.1).

---

## L. Problems Found

1. **`ValidationRuleEvaluator` was a silent no-op** (`APIPlatform.CrudEngine/Validation/`). It
   read via reflection for `IsRequired`/`ValidationRules` properties that don't exist on the real,
   frozen `FieldDefinition` (real shape: `FieldDefinition.Validation?.Required` /
   `MinLength`/`MaxLength`/`MinValue`/`MaxValue`/`RegexPattern`) — an assumption boundary left
   over from before SharedSchema was frozen, undiscovered because no existing test exercised it.
   **Fixed** (typed reads, no reflection) — this is what makes phase2.md 21 ("Name required")
   actually work.
2. **`APIPlatform.Playground`/`APIPlatform.Authentication` were never in `backend/EnterprisePlatform.sln`**
   — the "Full Solution BUILD PASS" claim in the Phase 1/2 prompts never actually built them.
   **Fixed** by adding both (plus the new test project) to the real solution file.
3. **`DefaultSqlDialectResolver` could never resolve when `AddDatabase()` + `AddCrudEngine()`
   were used together** — it demanded a raw `DatabaseOptions` constructor parameter, but
   `APIPlatform.Data`'s `AddDatabase()` only ever registers `IOptions<DatabaseOptions>`. This
   combination had never been exercised anywhere in the repo before Employee wiring; discovered
   only when the real SQL Server integration test first ran. **Fixed** (constructor now takes
   `IOptions<DatabaseOptions>`).
4. **Nothing in the repository implemented `ICurrentUser`/`ITenantContext`** (Foundation), which
   both `CrudEngine<T>` and Rbac's `DefaultAuthorizationContextFactory` require via constructor
   injection. **Fixed** with an application-level adapter (`HttpCurrentUserContextAdapter`)
   bridging Authentication's `ICurrentUserContext`; no platform file changed.
5. **Auth response envelope mismatch**: `AuthenticationController` returned the raw
   `AuthenticationResponse` model; `ui-platform-foundation`'s `apiRequest()` requires
   `{success,data,error}` and would throw on every response, including successful logins.
   **Fixed** with an app-level `ApiEnvelope<T>` wrapper (Auth + Employees controllers only).
6. **Refresh path/body mismatch and missing logout endpoint** (phase2.md 27's anticipated
   mismatches, confirmed real): frontend default `refreshPath` didn't match the backend's actual
   route; no `/logout` route existed at all. **Fixed**: `configureAuth({ refreshPath:
   '/auth/refresh' })` in the frontend, and a new `POST /api/auth/logout` action. The deeper
   refresh *behavior* (Problem 8 below) was left as documented, not fixed.
7. **No CORS configured anywhere in the platform.** **Fixed**: `AddCors`/`UseCors` in Playground,
   scoped to the dev frontend's origin only.
8. **`AuthenticationService.RefreshAsync` always returns `Ok=false`/`REAUTH_REQUIRED`, even for a
   valid refresh token** — by explicit design comment ("lightweight rotation only"), not a bug
   introduced here. **Not fixed** — out of scope per phase2.md 24 ("do not turn this into the
   full authentication modernization phase"); login + access token is sufficient to prove the
   CRUD/RBAC chain. Documented in Section N.
9. **`ui-platform-auth`/`ui-platform-forms`'s `peerDependencies` on `@nucleus/uiplatform-foundation`
   used a bare semver range instead of the workspace protocol**, so `pnpm install` tried (and
   failed) to fetch an unpublished private-scope package from the public npm registry. **Fixed**
   (`workspace:*`) — part of phase2.md 38's anticipated "known empty workspace configuration"
   fix, needed before any package could install at all.
10. **`frontend/package.json` and `frontend/pnpm-workspace.yaml` were literally empty (0 bytes)**
    — `pnpm install`/`pnpm build` failed immediately, unable to parse JSON. **Fixed**, populated
    only as far as needed to install/build the new test app.
11. **`ui-platform-forms` does not export `LayoutRenderer`** (only `Form`, which uses it
    internally when no `children` are supplied) — there is no public way to render the default
    field layout *and* add a submit button inside the same `<form>`. Worked around in the test
    app by rendering each field explicitly via the exported `<Field name=.../>` component
    (still the real registry/validation path) instead of relying on `Form`'s default layout.
    Not fixed in the package itself — a real gap worth flagging for a future Forms iteration.
12. **`usePermission`/`PermissionGuard` (ui-platform-auth) read from a JWT `"permission"` claim
    that `AuthenticationExecutionStage` never actually embeds** (confirmed by decoding a real
    issued token — only `nameidentifier`/`name`/`sub`/`email`/`iat`/`exp`/`iss`/`aud` are
    present). Client-side permission gating in the UI would therefore always show "denied" even
    for the admin user. **Not used** in the test app to avoid a misleading UI; the server-side
    403 is what the app surfaces instead. Documented, not fixed (would require Authentication
    changes out of this phase's scope).
13. **Only the first metadata validation error is ever surfaced** (`ValidationStage` takes
    `Errors.FirstOrDefault()`) — pre-existing platform behavior, not changed here; multiple
    simultaneous validation failures on one Employee only report the first.

---

## M. Changes Made

### Platform (minimal, targeted fixes — 2 files)
```text
backend/src/APIPlatform.CrudEngine/Validation/ValidationRuleEvaluator.cs   (rewritten: real property reads)
backend/src/APIPlatform.CrudEngine/Sql/Dialects/ISqlDialectResolver.cs    (ctor: IOptions<DatabaseOptions>)
```

### Build/workspace configuration (no runtime behavior change)
```text
backend/EnterprisePlatform.sln                       (added Playground, Authentication, Playground.Tests)
frontend/package.json                                 (was empty; populated as workspace root)
frontend/pnpm-workspace.yaml                          (was empty; populated)
frontend/packages/ui-platform-auth/package.json        (peerDep -> workspace:*)
frontend/packages/ui-platform-forms/package.json       (peerDep -> workspace:*)
```

### Test application — backend (`backend/playground/APIPlatform.Playground/`)
```text
APIPlatform.Playground.csproj          (+ CrudEngine, Nucleus.SharedSchema, Rbac references)
Models/Employee.cs                     (new)
Metadata/EmployeeEntityDefinitionProvider.cs   (new)
Defaults/EmployeeDefaultValueProvider.cs       (new)
Infrastructure/HttpCurrentUserContextAdapter.cs (new)
Infrastructure/ApiEnvelope.cs                  (new)
Migrations/EmployeeSqlServerMigration.cs        (new)
Extensions/EmployeeExtensions.cs               (new — AddEmployeeModule())
Services/EmployeeModuleInitializationService.cs (new — migrations + RBAC seeding at startup)
Controllers/EmployeesController.cs              (new)
Controllers/AuthenticationController.cs         (envelope wrap; + logout action)
Resolvers/PlaygroundIdentityResolver.cs         (+ viewer test user; TEST ONLY banner)
Program.cs                                      (AddEmployeeModule(); CORS)
```

### Test application — backend tests (`backend/playground/APIPlatform.Playground.Tests/`, new project)
```text
TestSupport/ (FakeDatabaseExecutor, FakeClock, FakeCurrentUser, EmployeeTestHost)
Unit/EmployeeCrudEngineTests.cs, Unit/EmployeeRbacTests.cs
Integration/EmployeeSqlServerIntegrationTests.cs
```

### Test application — frontend (`frontend/playground/ui-platform-playground/`, new app)
```text
package.json, tsconfig.json, vite.config.ts, index.html, src/styles.css
src/main.tsx, src/App.tsx, src/pages/LoginPage.tsx
src/employee/{types.ts, employeeSchema.ts, employeeApi.ts, EmployeeForm.tsx, EmployeeListPage.tsx}
```

---

## N. Remaining Issues

1. **Refresh token flow is still non-functional end-to-end** (`AuthenticationService.RefreshAsync`
   always returns `Ok=false`). Login works; a long-lived session that needs silent refresh does
   not. Fixing this for real requires changing `AuthenticationService`'s internal token-issuance
   logic — a platform (`APIPlatform.Authentication`) change explicitly out of this phase's scope.
2. **RBAC/SharedSchema field-level permission mapping** remains unbridged
   (`Nucleus.SharedSchema.Stub.FieldMetadata.DefaultPermissionKey` vs. the real
   `FieldDefinition.Permissions.{ReadRoles,WriteRoles}`) — explicitly deferred by phase2.md 23,
   still deferred here. Employee authorization is resource/action-level only.
3. **JWTs carry no `permission`/role claims**, so `ui-platform-auth`'s client-side
   `usePermission`/`PermissionGuard` are not meaningfully usable yet (Section L.12). Not
   addressed — would require Authentication changes.
4. **`ui-platform-forms` has no public `LayoutRenderer` export** — consumers who want the
   default section/group layout *and* custom chrome (like a submit button) around it currently
   have no supported way to do both (Section L.11).
5. **No frontend test runner exists** in any UIPlatform package. This phase's frontend
   verification relied on `tsc`/`vite build` plus a real, screenshotted browser drive rather than
   an automated test suite.
6. **An unrelated dev server on this machine was inadvertently stopped.** While freeing ports
   for this phase's own Vite dev server, a broad process-matching cleanup command also stopped
   an unrelated, already-running project's dev server (visible in an earlier screenshot capture
   as a page titled "IQS Forge", listening on ports 5173/5174, unconnected to this repository).
   It was not restarted, since its start command/working directory are unknown. **Flagging this
   directly so the user can restart it if needed** — this is the one action in this phase that
   affected something outside the repository/this task's scope.
7. During reconnaissance (before implementation began), a sub-agent inspecting the frontend
   packages ran `npm install` directly inside `ui-platform-foundation`, against instructions.
   The resulting `node_modules`/`package-lock.json` were found and deleted before `pnpm install`
   was run for real. No source files were affected.

---

## O. Phase 3 Recommendation

The architecture is proven. Recommended next steps, in priority order:

1. **Close Problem N.1** (refresh token) if any real session-length UI work is planned next —
   otherwise every UI phase after this one inherits the same "login only, no silent refresh"
   limitation.
2. **A UIPlatform Grid** — explicitly out of this phase's scope (phase2.md 30), but the plain
   HTML table in `EmployeeListPage` is a real gap for any second entity.
3. **A metadata-serving HTTP endpoint** (e.g. `GET /api/entities/{name}/definition`) so the
   frontend's `employeeSchema.ts` can be fetched instead of hand-mirrored — the manual mirror
   done here is explicitly a Phase 2 shortcut, not a pattern to repeat for every future entity.
4. Do **not** start Search, SignalR, Workflow, Storage, Dashboard, Nucleus Builder, or a second
   business entity until the above are triaged — consistent with phase2.md 40's stop condition.

**STOP condition reached.** This report does not begin any Phase 3 work.
