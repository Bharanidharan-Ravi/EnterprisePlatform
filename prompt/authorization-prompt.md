# EnterprisePlatform — Full Authorization (RBAC) Rollout: API + UI

**Purpose of this file:** single source of truth for the authorization initiative — page access, field
access, row/data-level visibility, posting/approval actions, and header/context scoping, driven by roles,
across both `backend/playground/APIPlatform.Playground` (API) and
`frontend/playground/ui-platform-playground` (UI).

**How to use this file in a new chat:** say *"implement phase N"* (or name a phase by its title). That
phase's **Objective / Scope / Steps / Playground Test** below is the complete spec for that unit of work —
read it, plus the "Established architecture & conventions" section, before writing any code. When a phase
is finished: flip its status to `DONE`, and append an **Implementation Record** under it (files touched,
what was verified, any bugs found) — same pattern Phase 0's record follows. Don't renumber phases once
named; add sub-items instead if scope grows.

**Golden rule established during Phase 0/1 discussion:** the UI is a UX convenience only (hide a button,
hide a field's input) — it enforces nothing, because anything client-side is fully under the user's
control. The API is the only real enforcement point: it must actually omit data the caller can't read and
actually reject writes/actions the caller can't perform. Every phase below follows that split.

---

## Status snapshot

| Phase | Title | Status |
|---|---|---|
| 0 | Identity ↔ RBAC bridge + durable role store | **DONE** |
| 1 | Field-level masking (API strip + UI consume) | **DONE** — Email only, per user request; see its Implementation Record |
| 2 | Row/data-level scoping | **DONE** |
| 3 | Posting/approval actions + Policy engine | NOT STARTED |
| 4 | Frontend page/route guarding (+ 403 view) | PARTIAL — button-level gating on Employees done; page/route guarding not done |
| 5 | Menu/navigation filtering | NOT STARTED |
| 6 | Role/permission administration (replace manual SQL edits) | NOT STARTED |
| 7 | Hardening (cache invalidation, audit logging, multi-tenant review) | NOT STARTED |

---

## Established architecture & conventions (read before touching code)

### Backend module split
- `APIPlatform.Authentication` — login/refresh/logout/session, JWT issuance. **Frozen v1**, don't redesign.
- `APIPlatform.Authorization` / `APIPlatform.Rbac` (`backend/src/APIPlatform.Authorization/APIPlatform.Rbac/`) —
  the RBAC engine. Deliberately has **zero** ASP.NET Core / `APIPlatform.Data` dependency. Resource types:
  `Api, Crud, Field, Row, Menu, Feature, Policy` (`Models/ResourceType.cs`). Deny always overrides allow.
  Role hierarchy via `Role.ParentRoleId`. `PermissionResolver` caches the resolved `PermissionSet` per
  `(tenantId, userId)` for 5 minutes (`IPermissionCache`).
- Everything RBAC-specific that touches ASP.NET Core, SQL, or this app's own tables lives in the
  **Playground composition root** (`backend/playground/APIPlatform.Playground/`), never inside the two
  packages above — that boundary is intentional, keep respecting it.

### Permission key convention
`{entityKey}.{action}` — lowercase entity key, e.g. `employee.read`, `employee.create`, `employee.update`,
`employee.delete`. Future action-style permissions (Phase 3) follow the same shape, e.g. `employee.post`,
`employee.approve` — no schema change needed, `AuthorizeAsync(entityKey, action)` already takes any string.

### Tenant
This host is single-tenant. The **only** tenant id that matters anywhere — seeding, enforcement, JWT
enrichment — is `HttpCurrentUserContextAdapter.TestTenantId` (`"default"`,
`backend/playground/APIPlatform.Playground/Infrastructure/HttpCurrentUserContextAdapter.cs`). Do **not**
use the JWT's `tenant_id` claim or `Logins.Dbname` (real value: `"IQS_DB"`) for any RBAC lookup — Phase 0
hit this exact bug (see its Implementation Record). `ITenantContext.TenantId` is hardcoded to
`TestTenantId` and ignores the JWT entirely; RBAC must always agree with that, not with the JWT.

### The identity↔RBAC bridge (Phase 0)
`IIdentityResolver` (Authentication's contract for "resolve a UserInfo") is decorated by
`RbacEnrichedIdentityResolver` (`backend/playground/APIPlatform.Playground/Resolvers/`), which fills
`UserInfo.RoleIds`/`PermissionIds` live from `IRoleService`/`IPermissionResolver` before `ClaimsBuilder`
turns them into JWT `role`/`permission` claims. Registered in `AuthenticationExtensions.cs`, wrapping
`LoginsIdentityResolver`. **Any new identity resolver must go through this same decorator** or its users
will get a JWT with no role/permission claims.

### The durable RBAC store (Phase 0)
Default `IRoleStore` (`InMemoryRoleStore`, from the Rbac package) is process-RAM-only and wiped on
restart — replaced for this app by `SqlServerRoleStore`
(`backend/playground/APIPlatform.Playground/Rbac/SqlServerRoleStore.cs`), registered **before**
`AddRbac()` in `EmployeeExtensions.AddEmployeeModule()` so the package's own `TryAddSingleton` skips its
default. Backed by 4 tables (migration: `Rbac/RbacSqlServerMigration.cs`, id `Rbac.Schema.v1`):

```
RbacRoles            (TenantId, Id, Name, ParentRoleId, IsSystemRole)          PK (TenantId, Id)
RbacUserRoles        (TenantId, UserId, RoleId)                                PK (TenantId, UserId, RoleId)
RbacPermissionGrants (Id, TenantId, RoleId?, UserId?, PermissionKey, Effect)   PK (Id)
RbacPolicyRules      (Id, TenantId, Name, PermissionKey, ResourceType, Priority) PK (Id)   -- not yet populated, reserved for Phase 3
```

Phase 2 added two more, in a **separate** migration (`Rbac/RbacRowScopeSqlServerMigration.cs`, id
`Rbac.RowScope.v1`, `Version = 2` — an applied migration is never edited in place):

```
RbacRowPermissionRules (Id, TenantId, EntityKey, FilterDelegateKey, TenantScoped) PK (Id)  + IX (TenantId, EntityKey)
RbacUserScopes         (TenantId, UserId, ScopeKey, ScopeValue)                   PK (TenantId, UserId, ScopeKey)
```

Phase 1 added one more, same reasoning (`Rbac/RbacFieldMaskSqlServerMigration.cs`, id
`Rbac.FieldMask.v1`, `Version = 3`):

```
RbacFieldPermissionRules (Id, TenantId, EntityKey, FieldKey, PermissionKey, Access) PK (Id)  + IX (TenantId, EntityKey)
```

`SqlServerRoleStore` is registered **Singleton** (so `PermissionResolver`'s existing Singleton lifetime
needs no change) but never constructor-injects the Scoped `IDatabaseExecutor` — it opens a short DI scope
per call via `IServiceScopeFactory`. Every write is `IF NOT EXISTS … INSERT` (idempotent), because
`EmployeeModuleInitializationService`'s seeding re-runs on every app boot. `IRoleStore` itself has no
"define a role" method, so a small extra interface, `IRoleDefinitionSeeder.EnsureRoleAsync(Role)`
(`Rbac/IRoleDefinitionSeeder.cs`), covers that — `SqlServerRoleStore` implements both.

**To assign a role to a real user today**, either add them to `EmployeeModuleInitializationService`'s
username-based seeding (currently only looks up Logins usernames `"admin"` → `employee-admin` and
`"viewer"` → `employee-viewer`), or insert a row into `RbacUserRoles` directly (`(TenantId, UserId,
RoleId)` = `('default', '<Logins.Id>', '<role id>')`) — this is exactly how the manual `laxmi` (Logins
`Id=2`) → `employee-viewer` test assignment was done. **Building a real way to do this without hand-SQL is
Phase 6.**

### Frontend package (`frontend/packages/ui-platform-auth`)
- `AuthGuard` — authentication-only route wrapper (redirects to `/login` if not authenticated). Has no
  concept of role/permission.
- `PermissionGuard` (`permission` / `any` / `all` props), `RoleGuard` (`role` / `any` props),
  `usePermission`/`useAllPermissions`/`useAnyPermission`, `useRole`/`useAnyRole` — all read straight out of
  `authStore`'s decoded JWT claims (`claims.permission[]`, `claims.role[]`), zero extra API calls. **UX-level
  only** — see golden rule above.
- Test page: `frontend/playground/ui-platform-playground/src/employee/EmployeeListPage.tsx`. As of Phase 0,
  New/Edit/Delete buttons are wrapped in `PermissionGuard permission="employee.create|update|delete"`.
  Field rendering (columns) is **not yet** permission-aware — that's Phase 1.

### CrudEngine's extension seam (what Phase 1/2 hook into)
`ICrudPipelineHook` (`backend/src/APIPlatform.CrudEngine/Hooks/ICrudPipelineHook.cs`) —
`OnBeforeAsync<TEntity>(CrudContext<TEntity>)` / `OnAfterAsync<TEntity>(CrudContext<TEntity>)`, registered
via DI (`services.AddCrudPipelineHook<THook>()`), all hooks run around every CRUD operation. Pipeline
order is `… ValidationStage → [OnBefore hooks] → ExecutionPlanningStage → ExecutionStage → [OnAfter
hooks] → ResponseMappingStage`, so a hook's `OnBeforeAsync` still runs before the List query is planned.

`CrudContext<TEntity>.RequestedFilters` stays `{ get; init; }` — Phase 2 added a **second, mutable
`AdditionalFilters` dictionary** next to it instead of making it settable, so "what the caller asked for"
stays immutable while "what the platform imposed" is separately inspectable. `ExecutionPlanningStage`
merges both into the plan, imposed-filter-wins on a key collision. See Phase 2's record for why.

### Test credentials (local dev DB — already committed in `appsettings.json`, not new exposure)
| User | Login | Password | Logins.Id | Role | Notes |
|---|---|---|---|---|---|
| Admin | `admin` | `0202` | `1` | `employee-admin` | Full CRUD (`read/create/update/delete`) + `employee.read.all`, so no row scoping |
| Viewer | `laxmi` | *(ask the user — not recorded here)* | `2` | `employee-viewer` | `read` only; manually assigned via `RbacUserRoles` insert, not by seeding code. Phase 2 gave them `department_id = 'wed'` (`RbacUserScopes`), so they see only that department's rows |
| Scoped viewer | `salesviewer` | `Scope#2026` | `rowscope-tester` | `employee-viewer` | Created during Phase 2 verification via `POST /api/auth/register`, so row scoping is testable without needing `laxmi`'s password. `department_id = 'Sales'` |

Two more identities exist only for automated tests (`EmployeeRbacTests.cs`), never reachable via a real
login: `user-123` (admin) / `user-456` (viewer), tied to the unused `PlaygroundIdentityResolver`.

### How to run/verify locally (used for every phase's verification so far)
```bash
# Build + full test suite
cd backend/playground/APIPlatform.Playground && dotnet build
cd backend/playground/APIPlatform.Playground.Tests && dotnet test

# Run the API standalone against the real DB
cd backend/playground/APIPlatform.Playground
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5099 dotnet run --no-build --no-launch-profile

# Log in and inspect claims
curl -s -X POST http://localhost:5099/api/auth/login -H "Content-Type: application/json" \
  -d '{"loginIdentifier":"admin","password":"0202"}'
curl -s http://localhost:5099/api/auth/me -H "Authorization: Bearer <accessToken>"

# Inspect RBAC tables directly
"/c/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/sqlcmd" -S ANARA -U sa -P 0202 -C -d Nucleus -Q "SELECT * FROM RbacPermissionGrants;" -W

# Frontend typecheck
cd frontend/playground/ui-platform-playground && npm run typecheck
```
Kill the dev server afterward (`Get-NetTCPConnection -LocalPort 5099 | Select OwningProcess` → `Stop-Process`)
— it isn't left running between sessions.

### Reference: the role ladder every phase should stay expressible against
Extreme → normal, using Employee data as the concrete example. Nothing below requires a schema change —
`PermissionKey` strings + `FieldPermissionRule` + `RowPermissionRule` + `PolicyRule` already cover it, they
just need wiring (that's what Phases 1–3 are).

| Role | Page access | Field access | Row/data scope | Posting/actions |
|---|---|---|---|---|
| Super Admin | every page, every app | every field incl. audit/system | all tenants, no filter | anything, bypass limits |
| Tenant Admin | every page in tenant + admin screens | every field in tenant | all companies/branches in tenant | full CRUD + manage roles |
| Branch/Company Manager | Employee, Payroll, Reports | sees salary for own branch only | `BranchId == user.branch_id` | approve up to a limit policy |
| Department Head/Approver | Employee (own dept), Approvals | own-dept cost fields only | `DepartmentId == user.department_id` | post/approve own team's docs, no delete |
| Regular Employee | My Profile, limited create form | no Salary/SSN; edit only own non-restricted fields | `EmployeeId == self` | create draft only |
| Read-only Auditor | broad read, no action buttons | sees everything (compliance) | tenant-wide read | zero write |
| External/Vendor Guest | one narrow portal page | whitelisted subset only | `VendorId == self` | read-only |
| Anonymous | `/login` only | n/a | n/a | n/a — already fully works |

---

## Phase 0 — Identity ↔ RBAC bridge + durable role store — **DONE**

### Objective
Make role/permission data actually reach the JWT (so UI guards have real data), and make that data survive
a restart (so it's not re-invented every boot).

### Implementation Record
- **New:** `Resolvers/RbacEnrichedIdentityResolver.cs`, `Rbac/RbacSqlServerMigration.cs`,
  `Rbac/SqlServerRoleStore.cs`, `Rbac/IRoleDefinitionSeeder.cs`,
  `APIPlatform.Playground.Tests/Unit/RbacEnrichedIdentityResolverTests.cs` (5 tests).
- **Edited:** `Extensions/AuthenticationExtensions.cs` (decorator registration),
  `Extensions/EmployeeExtensions.cs` (durable store + migration registration, before `AddRbac()`),
  `Services/EmployeeModuleInitializationService.cs` (seeding generalized to work against
  `InMemoryRoleStore` or any `IRoleDefinitionSeeder`; also seeds real `Logins` rows named
  `admin`/`viewer` by username, not just the two hardcoded test ids),
  `frontend/.../EmployeeListPage.tsx` (New/Edit/Delete wrapped in `PermissionGuard`).
- **Real bug caught by live testing, not just unit tests:** first version of the decorator used
  `user.TenantId` (real value `"IQS_DB"`, from `Logins.Dbname`) when non-null, instead of always using
  `HttpCurrentUserContextAdapter.TestTenantId`. Since request-time enforcement always uses `TestTenantId`
  regardless of the JWT, this silently produced empty `role`/`permission` claims for the real `admin`
  login even though every unit test (which only ever used `"default"`) passed. Fixed; see the tenant
  convention note above.
- **Verified:** full suite 26/26 green; built + ran the real API against the real SQL Server DB; confirmed
  via `sqlcmd` all 4 tables exist with correct rows; restarted the API a second time and confirmed row
  counts unchanged (idempotency); logged in as the real `admin` user twice (before/after the durable-store
  switch) and decoded the JWT both times — `role: employee-admin`, all 4 `permission` claims present both
  times; confirmed the same via `GET /api/auth/me`.
- **Also fixed:** `EmployeeListPage.tsx` was rendering New/Edit/Delete unconditionally for any
  authenticated user (a real gap surfaced by testing with the `laxmi`/viewer account) — wrapped each in
  `PermissionGuard`, keyed to the exact same permission strings the backend already grants/checks.
  Confirmed correct server-side behavior was never actually at risk (`EmployeeRbacTests.
  ViewerUser_IsDenied_ForWriteActions` already covered it); this was a UI-only defect.

---

## Phase 1 — Field-level masking (API strip + UI consume) — **DONE**

### Objective
A field can be sensitive independent of whether the whole entity is readable — e.g. a viewer can read an
Employee record but must never receive its `Salary`/`SSN`-style field value at all, not just have it
hidden client-side.

### Scope
**Backend (does the real work):**
1. Add a field worth masking to `Employee` — either repurpose `Department`, or (cleaner for a real demo)
   add a new `Salary` column: model (`Models/Employee.cs`), migration bump
   (`Migrations/EmployeeSqlServerMigration.cs` — new `IMigration` with `Version` after `1`, don't edit the
   already-shipped v1 migration), entity metadata provider, form/list UI.
2. Seed `FieldPermissionRule` rows for it: `employee-admin` → `Access.Write` (or `Read`), `employee-viewer`
   → `Access.None` (or simply no rule at all — absence means "no extra restriction beyond entity-level",
   per `FieldPermissionRule`'s own doc comment, so an explicit `None` rule is what actually hides it).
   `IFieldPermissionRuleStore` also defaults to an in-memory implementation
   (`InMemoryFieldPermissionRuleStore`) — decide whether Phase 1 also needs a durable version (recommended:
   yes, same pattern as `SqlServerRoleStore`, for consistency) or whether an in-memory version is
   acceptable for this phase (defer durability to Phase 6/7 if so — call this explicitly in the
   Implementation Record either way). **Phase 2 already answered the equivalent question for rows with
   "yes, durable"** — copy that pattern (`Rbac/SqlServerRowPermissionRuleStore.cs` +
   `Rbac/RbacRowScopeSqlServerMigration.cs`, registered before `AddRbac()` in `AddEmployeeModule()`).
3. Wire an `ICrudPipelineHook` (new class, e.g. `FieldMaskCrudHook`, registered in
   `EmployeeExtensions.AddEmployeeModule()`) that:
   - `OnAfterAsync` (Get/List): calls `IFieldAuthorizationService.GetFieldMaskAsync(entityKey)`, and for
     every field whose `FieldAccess` is `None`, nulls/removes it from `context.Entity` /
     `context.ExecutionResult` before the response is built. The value must not reach the JSON payload —
     verify this by inspecting the raw HTTP response body, not just what the UI renders.
   - `OnBeforeAsync` (Create/Update): rejects (or silently drops, decide and document which) a write to
     any field whose `FieldAccess` is not `Write`.
4. Decide and document: is `EmployeesController` itself where the mask gets applied (simplest, matches how
   `ICrudAuthorizationService` is already called directly there), or does it belong purely in the
   `ICrudPipelineHook` so every future entity gets it for free without controller boilerplate? Prefer the
   hook — it's the platform's own designed extension point and avoids repeating this per controller.

**Frontend (renders what the API actually sent — no separate hardcoded field-visibility rule set):**
5. `EmployeeListPage.tsx`: the masked field's column should simply handle the value being `null`/absent
   gracefully (e.g. render `—`) — it should **not** duplicate the RBAC rule client-side (no `if
   (role === 'viewer') hideColumn` logic; the API already decided this).
6. `EmployeeForm.tsx`: for a field the user has no `Write` access to (but might have `Read`), render it
   read-only/disabled rather than omitting the input entirely, if the API still returns the value for
   Read; if the API omits it entirely (no Read either), omit the input too.

### Playground test
1. Log in as `admin`, `GET /api/employees/{id}` — confirm the masked field's real value is present.
2. Log in as `laxmi` (viewer), `GET /api/employees/{id}` — confirm the masked field is `null`/absent **in
   the raw JSON**, not just hidden by the UI. Use `curl`, not the browser, to be sure.
3. As `laxmi`, attempt `PUT /api/employees/{id}` including a value for the masked field — confirm it's
   rejected or silently ignored (per whatever Step 3's `OnBeforeAsync` decision was), and confirm the
   stored value in SQL didn't change.
4. In the UI, confirm the column/input reflects the same thing the raw API calls showed — this proves the
   UI is just rendering what the API sent, not enforcing its own copy of the rule.
5. Re-run the full backend test suite + frontend typecheck; add a test proving the masking (mirrors
   `EmployeeRbacTests`/`RbacEnrichedIdentityResolverTests` style).

### Implementation Record

**Scope actually built:** the user's ask was narrow and concrete — "show a mail id only to admin,
others don't want to view" — so Step 1 (add a new `Salary` column) was skipped; the existing
`Email` field was used as the masked field instead. Every other step was built as specified.

**New — app-specific (`backend/playground/APIPlatform.Playground/Rbac/`):**
`RbacFieldMaskSqlServerMigration.cs` (Version 3, id `Rbac.FieldMask.v1` — a new migration, per
Phase 2's precedent, not an edit to an already-shipped one), `SqlServerFieldPermissionRuleStore.cs`,
`FieldMaskCrudHook.cs`, `EmployeeFieldMasks.cs` (constants, mirrors `EmployeeRowFilters.cs`'s
naming role — field masking needs no named-delegate registry, since `FieldMaskDescriptor.FromRules`
is pure data). Tests: `TestSupport/FieldMaskTestHost.cs`, `Unit/EmployeeFieldMaskTests.cs` (7 tests,
including one proving composition with Phase 2's row scoping on the same `List` call).

**Edited:** `Extensions/EmployeeExtensions.cs`, `Services/EmployeeModuleInitializationService.cs`,
`frontend/.../employee/types.ts`, `EmployeeListPage.tsx`, `EmployeeForm.tsx`.

**Step 2 — durability: SQL-backed, following Phase 2's precedent directly.**
`SqlServerFieldPermissionRuleStore` replaces `InMemoryFieldPermissionRuleStore` for the identical
reason: losing a field rule on restart fails **open** (no rule = "no additional restriction" per
`FieldPermissionRule`'s own doc comment = Email visible to everyone). Same shape as
`SqlServerRowPermissionRuleStore` — Singleton + `IServiceScopeFactory`, idempotent `IF NOT EXISTS …
INSERT`, registered before `AddRbac()`. This closes Phase 7's remaining "field half" durability item.

**One rule, not two.** `FieldMaskDescriptor.FromRules` assigns `dict[FieldKey] = grantedByPermission
? rule.Access : FieldAccess.None` per rule it's given, as a plain overwrite — so two rules for the
same field (e.g. one saying "granted → Write", another saying "granted → None" for a different
permission key) would have the later-processed one silently win, in whatever order the store
happens to return them. That's not composable, so this only ever seeds **one** rule per field: one
`(EntityKey=employee, FieldKey=Email, PermissionKey=employee.email.read, Access=Write)`. Holding
`employee.email.read` (granted only to `employee-admin`) yields `Write`; not holding it — the
default for every other role — falls through the same rule's own `else` branch to `None`. No
second, unmasking rule needed or possible under this contract's actual semantics.

**Step 3 — `OnBeforeAsync` write-blocking was intentionally not built,** and this is a real, logged
gap rather than an oversight. The natural implementation — reject a write to a field the caller
only holds `Read`/`None` on — needs a hook to short-circuit the pipeline from `OnBeforeAsync`, and
`CrudPipeline<TEntity>.RunAsync` never re-checks `CrudContext.ShortCircuited` after the
`OnBeforeAsync` hook loop (only after `ValidationStage`), so setting it there is silently a no-op
today — the same class of structural gap Phase 2 found and fixed for `RequestedFilters`. Not
exploitable in this host: the only role that can currently reach Create/Update at all
(`employee-admin`) also holds Write on the one masked field, by construction of the seeded grants.
Logged as a Phase 7 item (see below) rather than fixed here, since fixing it now would mean adding
pipeline-level short-circuit machinery to satisfy a scenario (a write-capable-but-field-restricted
role) that doesn't exist in this host yet — narrower than what was actually asked.

**Frontend:** `Employee.email` retyped `string | null`; `EmployeeListPage` renders `emp.email ??
'—'`; `EmployeeForm`'s `defaultValues.email` falls back to `''` on null (defensive only — the form
is unreachable by a non-admin today, since `PermissionGuard permission="employee.update"` already
gates it, and every role holding `employee.update` also holds Email `Write`). No rule duplicated
client-side.

**Verified — full suite 48/48 green** (was 41; +7 field-mask), frontend `npm run typecheck` clean,
and the real API run against the real SQL Server DB:
- Migration `Rbac.FieldMask.v1` applied; `RbacFieldPermissionRules` has the one seeded row; admin's
  grants include `employee.email.read`. Restarted a second time — row/grant counts unchanged
  (idempotent: 1 field rule, 7 admin grants both times).
- `GET /api/employees` and `GET /api/employees/{id}` as `admin` → real email present in the raw JSON.
- Same calls as `salesviewer` (Phase 2's scoped viewer account) → **`"email":null`** in the raw
  JSON, every other field intact, on both the list and the single-row response.
- Confirmed row scoping and field masking compose correctly on one response: `salesviewer`'s list
  was simultaneously filtered to their own department **and** had `email:null` on the row that
  remained.

**Phase 7 addition (new item):** fix `CrudPipeline<TEntity>.RunAsync` to re-check
`CrudContext.ShortCircuited` after the `OnBeforeAsync` hook loop, then implement
`FieldMaskCrudHook.OnBeforeAsync` to reject a Create/Update touching a field the caller doesn't
hold `Write` on. Needed before any role gets Create/Update without also holding Write on every
masked field on that entity.

---

## Phase 2 — Row/data-level scoping — **DONE**

### Objective
A role should only ever see the rows it's entitled to — e.g. a Branch Manager's `GET /api/employees` list
returns only their branch's employees, not the whole tenant's.

### Scope
1. Resolve the structural blocker: `CrudContext<TEntity>.RequestedFilters` is init-only. Either (a) make it
   settable / add a mutable `AdditionalFilters` dictionary a hook can populate before the query executes,
   or (b) resolve the row filter earlier, inside `ICrudEngine.ListAsync`/`GetAsync`, before `CrudContext` is
   built. Pick one, document why, implement it in `APIPlatform.CrudEngine`.
2. Register a real filter delegate in `IRowFilterRegistry` (e.g. `"OwnDepartment"` →
   `DepartmentId == ctx.department_id`) — this is app-supplied logic per Rbac's own design (`RowFilterDescriptor`'s
   doc comment: "Rbac never contains a domain predicate itself").
3. Populate `department_id`/`branch_id`/`company_id` for real — today no `IIdentityResolver` sets them
   (verify still true before starting; `LoginsIdentityResolver` currently only reads `Id/Username/.../Dbname`
   from `Logins`). Likely needs joining to whatever table actually holds an employee's own
   department/branch (maybe the `Employees` table itself, if `UserId` can be correlated to an `Employee`
   row) — investigate before assuming a schema change is required.
4. Attach a `RowPermissionRule` per entity+role and call `IRowAuthorizationFilterProvider.GetRowFilterAsync`
   from wherever Step 1 lands, merging its `RowFilterDescriptor` into the query.
5. Same durability question as Phase 1: `InMemoryRowPermissionRuleStore` vs. a SQL-backed one — decide and
   document.

### Playground test
1. Seed a second Employee row with a different `Department` value than the first.
2. Assign a role scoped to only one department; log in as that role's user.
3. `GET /api/employees` — confirm only the in-scope row comes back, both via `curl` (raw JSON) and the UI list.
4. Confirm `GET /api/employees/{id}` for an out-of-scope row is denied/not-found (decide which — probably
   `404`, not `403`, so scoping doesn't leak the existence of out-of-scope rows).
5. Confirm an in-scope-role user (e.g. admin/no row filter) still sees everything.

### Implementation Record

**New — platform (`backend/src/APIPlatform.Authorization/APIPlatform.Rbac/`):**
`Contracts/IUserScopeStore.cs`, `Models/ScopeKeys.cs`, `Stores/InMemoryUserScopeStore.cs`.

**New — app-specific (`backend/playground/APIPlatform.Playground/Rbac/`):**
`RbacRowScopeSqlServerMigration.cs`, `SqlServerRowPermissionRuleStore.cs`,
`SqlServerUserScopeStore.cs`, `EmployeeRowFilters.cs`, `RowScopeCrudHook.cs`. Tests:
`TestSupport/RowScopeTestHost.cs`, `Unit/EmployeeRowScopeTests.cs` (13 tests).

**Edited:** `CrudEngine/Models/CrudContext.cs`, `CrudEngine/Pipeline/Stages/ExecutionPlanningStage.cs`,
`Rbac/Services/DefaultAuthorizationContextFactory.cs`, `Rbac/DependencyInjection/ServiceCollectionExtensions.cs`,
`Playground/Extensions/EmployeeExtensions.cs`, `Playground/Extensions/AuthenticationExtensions.cs`,
`Playground/Resolvers/RbacEnrichedIdentityResolver.cs`,
`Playground/Services/EmployeeModuleInitializationService.cs`,
`Tests/Unit/RbacEnrichedIdentityResolverTests.cs` (+2 tests),
`frontend/.../employee/EmployeeListPage.tsx`.

**Post-review correction — moved the scope-storage contract into `APIPlatform.Rbac`.** The first
pass put `IUserScopeStore` and a `ScopeAwareAuthorizationContextFactory` decorator entirely in
Playground, on the reasoning that Rbac "carries claims as opaque strings and never asks where they
come from." That's true of the *predicate* (`EmployeeRowFilters`, correctly Playground-only — see
below), but not of the *sourcing mechanism*: neither class had a line of Employee-specific code,
and every other Rbac abstraction (`IRoleStore`, `IFieldPermissionRuleStore`,
`IRowPermissionRuleStore`) already follows "package defines the interface + ships an in-memory
default, app overrides with a durable one." Fixed by moving `IUserScopeStore` + `ScopeKeys` +
`InMemoryUserScopeStore` into Rbac (registered via `AddRbac()`'s existing `TryAddSingleton`
pattern), and folding the merge logic directly into Rbac's own `DefaultAuthorizationContextFactory`
— which also means that factory no longer leaves `AuthorizationContext.Claims` empty for *any*
consuming app, not just this one. Net effect: **`ScopeAwareAuthorizationContextFactory` is gone
entirely** — Playground now only supplies `SqlServerUserScopeStore` and registers nothing else for
this concern. `RowScopeCrudHook` stayed in Playground on review: it bridges `CrudEngine` and Rbac,
which are deliberately unaware of each other, so — same as `EmployeesController` calling
`ICrudAuthorizationService` directly — it has nowhere else to live under the current package
boundaries. Re-verified live against SQL Server after the move; behavior unchanged (see Verified,
below). `backend/tests/APIPlatform.Rbac.Tests` (11 tests) and `Nucleus.TestHarness.Rbac` — both
pre-existing, neither touched by Phase 2 — stayed green throughout, since both call plain
`AddRbac()` with no factory override to invalidate.

**Step 1 — the init-only `RequestedFilters` blocker: chose (a), a mutable `AdditionalFilters`
dictionary.** Option (b) — resolving the filter inside `ICrudEngine.ListAsync/GetAsync` before the
context is built — was rejected because it would put authorization knowledge in `CrudEngine<T>`'s
entry point, i.e. make `APIPlatform.CrudEngine` reference Rbac, which the module split forbids.
Within (a), a *second* dictionary rather than a settable `RequestedFilters`: the caller's request
stays immutable and auditable for the whole pipeline, and the two sources stay distinguishable.
`ExecutionPlanningStage` merges caller-first-then-imposed, so **an imposed filter overwrites a
caller's value for the same field** — a caller must not be able to widen a security scope by naming
the field itself. (Not a full AND of two predicates on one column: `FilterClauseBuilder` holds one
value per field, so a genuine conflict resolves to the security value rather than to an empty set.
Can't arise today — `EmployeesController` only ever passes `EmployeeCode` — and is covered by
`CallerSuppliedDepartmentFilter_CannotOverrideTheScopeFilter`.)

**Steps 2+4 — the filter, and where it's applied.** `EmployeeRowFilters.OwnDepartmentAsync` is
registered in `IRowFilterRegistry` under `"OwnDepartment"` at startup, and
`RbacRowPermissionRules` carries one seeded rule (`employee` → `OwnDepartment`). Applied by
`RowScopeCrudHook` (`ICrudPipelineHook`), *not* by `EmployeesController` — the hook is generic over
`TEntity` and keys off `context.EntityName`, so every future entity gets scoping for free the moment
a rule row exists for it, and entities with no rule are untouched. Convention this host defines:
`RowFilterDescriptor.Parameters` is a `{column → required value}` map, ANDed as equality; the hook is
its only reader (Rbac stays predicate-agnostic, per `RowFilterDescriptor`'s own doc comment).
- *List* → `OnBeforeAsync` pushes the parameters into `AdditionalFilters`, so the scope is part of
  the generated `WHERE` and out-of-scope rows are never read out of SQL Server at all.
- *GetByKey* → `OnAfterAsync` discards a non-matching row (`ExecutionResult = null`), which
  `EmployeesController` maps to **404, not 403** — deliberately indistinguishable from a nonexistent
  id, so scoping doesn't leak that the row exists.
- *Update/Delete* are covered **transitively**: the controller loads through `GetAsync` first, which
  404s an out-of-scope id before any write is planned. Verified live (see below), and asserted as an
  explicit boundary by `UpdateItself_IsNotRowScoped_SoTheControllersPreLoadIsWhatEnforcesIt`. A
  future controller that mutates without loading first would not be covered.

**"Admins see everything" is a permission, not a rule-attachment.** `IRowPermissionRuleStore
.GetRulesAsync(tenantId, entityKey)` takes no role, so a rule can't be attached to some roles and not
others. The delegate itself checks for `employee.read.all` (seeded to `employee-admin`) and returns
`RowFilterDescriptor.None` for holders. Fail-closed the other way: a scoped user with no
`department_id` yields `Department = NULL`, which matches nothing, so they see zero rows rather than
all of them.

**Step 3 — where `department_id` actually comes from. Investigated first, as the spec asked:**
`[Logins]` has no department/branch/company column (columns confirmed via `INFORMATION_SCHEMA`), and
`[Employees]` has no column correlating a row back to a Logins user — so there was no existing table
to derive this from and a schema addition was genuinely required. Added `RbacUserScopes`, deliberately
generic (`ScopeKey`/`ScopeValue`, not three fixed columns) so branch/company scoping needs no further
schema. Two consumers, and the split matters:
- **Enforcement** reads it live per request via Rbac's own `DefaultAuthorizationContextFactory`,
  which now merges `IUserScopeStore` into `AuthorizationContext.Claims` (see the post-review
  correction above — that gap used to mean *no* filter delegate had anything to read, for any app).
- **The JWT** gets it via `RbacEnrichedIdentityResolver` → `UserInfo.DepartmentId` → `ClaimsBuilder`
  (which already knew how to emit `department_id`; it had simply never been given a value). This is
  for the UI's benefit only — enforcement never reads the token, so a stale claim widens nobody's
  access. Same Phase-0 lesson as the tenant bug: enforcement and the store must not be able to
  disagree. Deliberately **not cached** — a second independently-stale cache next to
  `PermissionResolver`'s 5-minute one would let a user's department disagree with their permissions,
  for one indexed PK seek per request.

**Step 5 — durability: both stores are SQL-backed, not in-memory.** `SqlServerRowPermissionRuleStore`
replaces `InMemoryRowPermissionRuleStore` for the same reason Phase 0 replaced `InMemoryRoleStore`:
losing a row rule on restart fails **open** (no rule → no filter → every row visible). Same
Singleton + `IServiceScopeFactory` shape, same idempotent `IF NOT EXISTS … INSERT` writes, registered
before `AddRbac()`. This closes Phase 7's item 4 for rows; the *field* half stays open for Phase 1.

**Seeding policy:** the init service seeds **rules only** ("Employee is scoped by OwnDepartment",
"`employee-admin` is exempt"), never per-user scope *values* — which department a real person is in
is user data, not module config, and belongs to Phase 6. Registering the filter delegates is *not*
wrapped in the seeding try/catch: a swallowed registration failure would mean no filter resolves and
every scoped user silently sees everything.

**Verified — full suite 41/41 green** (was 26; +13 row-scope, +2 resolver), frontend `npm run
typecheck` clean, and the real API run against the real SQL Server DB:
- Migration `Rbac.RowScope.v1` applied; both new tables exist; seeded `employee`→`OwnDepartment` rule
  and `employee.read.all` grant present. Restarted a second time — row counts unchanged (idempotent).
- Test data: created a second Employee (`SALES-1`, dept `Sales`) through the API as `admin`, alongside
  the existing dept-`wed` row. Registered a dedicated scoped login `salesviewer` (Logins Id
  `rowscope-tester`) via `POST /api/auth/register` rather than needing `laxmi`'s password.
- `salesviewer`'s JWT carries `"department_id":"Sales"` — the claim that had always been absent.
- **`GET /api/employees` as `salesviewer` returned only the `Sales` row, in the raw JSON**; as `admin`
  it returned both.
- `GET /api/employees/{wed-id}` as `salesviewer` → **404** (in-scope id → 200; same id as `admin` → 200).
- Write path: granted `salesviewer` `employee.update` directly and restarted (to clear the 5-minute
  permission cache) — `PUT` on the out-of-scope row → **404**, and `sqlcmd` confirmed the row was
  **unchanged**; `PUT` on the in-scope row → 200. Temporary grant deleted afterwards.
- `laxmi` (Logins Id `2`, the documented viewer account) was given `department_id = 'wed'` by the same
  hand-SQL route Phase 0 used for its role, so the account still returns rows rather than being
  silently emptied by the new fail-closed default.
- **Re-verified after the post-review move into `APIPlatform.Rbac`** (see Implementation Record
  above): full suite still 41/41, plus `APIPlatform.Rbac.Tests` (11/11, pre-existing) and
  `Nucleus.TestHarness.Rbac` both green — neither needed a code change, since both call plain
  `AddRbac()`. Re-ran the same `salesviewer`/`admin` List and GetByKey checks against the live DB
  post-refactor; identical results.

**Known limitation, logged for Phase 7 rather than worked around:**
`IRowAuthorizationFilterProvider.GetRowFilterAsync` returns `RowFilterDescriptor.None` both for "no
rule applies" and for "the caller was denied `Row.Read`" — the two are indistinguishable through that
contract, so a denied caller would get an *unscoped* query. Not reachable today:
`EmployeesController` checks `employee.read` via `ICrudAuthorizationService` and returns 403 before
CrudEngine is ever entered. Closing it properly means surfacing `AuthorizationResult.Allowed` from
that provider, which is an `APIPlatform.Rbac` package change.

**Frontend:** no rule is duplicated client-side (the list renders exactly what the API sent).
`EmployeeListPage` gained two display-only touches: the header notes which department the caller is
seeing, read from the existing `useCurrentUser().departmentId`, and the table now renders an explicit
empty-state row — scoping makes "zero rows" a normal outcome that would otherwise read as a broken page.

---

## Phase 3 — Posting/approval actions + Policy engine

### Objective
Some actions are conditional on more than "do you hold this permission key" — e.g. "can approve only if
amount < a limit" or "cannot approve your own submission."

### Scope
1. Introduce at least one non-CRUD action permission, e.g. `employee.post` or (better demo) add a small
   second entity/workflow-ish concept if Employee doesn't naturally have a "post" action — decide based on
   what exists at implementation time; don't force a fake action onto Employee if it doesn't fit.
2. Register a named policy in `IPolicyRuleRegistry` (e.g. `"AmountUnderApprovalLimit"`,
   `"NotSelfApproval"`) — actual boolean logic is app-supplied, per `PolicyRule`'s own doc comment.
3. Attach the `PolicyRule` to the relevant `PermissionKey`; confirm `IPolicyEngine.EvaluateAsync` actually
   gets invoked from the pipeline's `ExecutionStage` for a request carrying that permission key (verify —
   Phase 0 build already showed `PermissionResolutionStage` collects policy rule *definitions*; confirm
   `ExecutionStage` is really where they're *evaluated*, per the Rbac pipeline doc comments).
4. Seed `RbacPolicyRules` rows (the table already exists from Phase 0's migration, currently empty).

### Playground test
1. Grant a role the `employee.post`-style permission plus a policy limiting it (e.g. an amount field under
   a threshold).
2. Confirm an in-limit action succeeds, an out-of-limit one is denied, for the same user/role — proving the
   policy, not just the permission key, is actually being evaluated.

---

## Phase 4 — Frontend page/route guarding (+ 403 view)

### Objective
Whole pages, not just buttons/fields, should be role-gated — e.g. a viewer should never even reach a
"Manage Roles" page, and hitting a URL directly for a page they lack permission for should show a clear
403, not a broken/empty page.

### Scope (button-level gating on Employees already done in Phase 0 — don't redo it)
1. Build a `PermissionRoute`/`RoleRoute` wrapper (similar shape to `AuthGuard`, composable with it) in
   `ui-platform-auth`, taking a `permission`/`role` prop, redirecting to a 403 view (not `/login`) when
   authenticated-but-not-authorized.
2. Add a minimal 403 view/component.
3. Apply it to `App.tsx`'s route table once there's more than one page worth gating (today there's only
   `/login` and `/`, both intentionally reachable by anyone authenticated) — this phase's real payoff shows
   up once Phase 6 (an admin screen) exists; consider sequencing Phase 6 before finishing this phase's
   route application if there's nothing else to gate yet.

### Playground test
1. As a role without some new permission, navigate directly to a gated route's URL — confirm the 403 view,
   not a crash or an empty page.
2. As a role with it, confirm normal access.

---

## Phase 5 — Menu/navigation filtering

### Objective
A rendered nav/menu should only list pages/actions the current user can actually use —
`IMenuAuthorizationService.FilterMenuAsync` already exists for this, unused.

### Scope
1. Define a real `MenuItem` tree for the Playground app (today there's no nav component at all — `App.tsx`
   is two hardcoded `<Route>`s).
2. Expose it through an endpoint, e.g. `GET /api/me/menu`, running the tree through
   `IMenuAuthorizationService.FilterMenuAsync`.
3. Build a small nav component in the UI consuming that endpoint.

### Playground test
Log in as two different roles, confirm the returned/rendered menu differs correctly for each.

---

## Phase 6 — Role/permission administration

### Objective
Replace "insert a row into `RbacUserRoles` by hand in SSMS" with a real, safe way to assign roles/grants.

### Scope
1. Minimal API endpoints wrapping `IRoleStore`/`IRoleService`: assign a role to a user, grant/revoke a
   permission, list a user's effective roles — all already exist as methods, just need HTTP surface +
   their own authorization (who can call *these* endpoints — almost certainly a very small admin-only set).
2. A minimal UI screen for the above (can be very rough — this is a Playground, not a product).
3. Decide whether `EmployeeModuleInitializationService`'s hardcoded username-based seeding should be
   retired once this exists, or kept as a dev convenience.

### Playground test
Use the new endpoints/UI to assign `laxmi` a role instead of hand-editing SQL; confirm it behaves
identically to the manual insert used during Phase 0 testing.

---

## Phase 7 — Hardening

### Objective
Close the gaps that are fine for a Playground but not for anything beyond it.

### Scope
1. **Cache invalidation:** `RoleService.GrantPermissionAsync`'s known v1 limitation — revoking a role's
   permission only invalidates cache for a directly-specified `UserId`, not every user holding that role
   (no user↔role reverse index). Needs that index, likely in `SqlServerRoleStore` or a new component
   `RoleService` can consult.
2. **Audit logging:** implement `IAuthorizationHook.OnDeniedAsync` (and optionally the other two hook
   methods) to log every denial somewhere durable — currently unimplemented, denials vanish silently.
3. **Multi-tenant review:** everything above was built and tested against a single fixed tenant
   (`"default"`). If this app (or a sibling generated app) ever needs real multi-tenancy, re-audit every
   place `HttpCurrentUserContextAdapter.TestTenantId` was hardcoded.
4. ~~Revisit whether `InMemoryRowPermissionRuleStore`/`InMemoryFieldPermissionRuleStore` need the same
   durable-store treatment `IRoleStore` already got in Phase 0~~ — done: rows in Phase 2
   (`SqlServerRowPermissionRuleStore`), fields in Phase 1 (`SqlServerFieldPermissionRuleStore`).
5. **Row-filter fail-open branch (found in Phase 2):** `IRowAuthorizationFilterProvider.GetRowFilterAsync`
   returns `RowFilterDescriptor.None` for both "no rule applies" and "caller denied `Row.Read`", so a
   denied caller would get an unscoped query. Unreachable today (the controller 403s first), but the
   contract should surface `AuthorizationResult.Allowed` so callers can fail closed on their own.
6. **Hooks can't short-circuit the pipeline (found in Phase 1):** `CrudPipeline<TEntity>.RunAsync` only
   checks `CrudContext.ShortCircuited` after `ValidationStage`, never after the `OnBeforeAsync` hook
   loop — so a hook (e.g. a future `FieldMaskCrudHook.OnBeforeAsync` rejecting a masked-field write)
   cannot actually stop execution by setting it. Add the same check after the hook loop, then implement
   field-mask write-rejection on top of it. Not exploitable today: the only role that can currently
   reach Create/Update also holds Write on every masked field.
