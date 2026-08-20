# EnterprisePlatform — Phase 1 Foundation Repair

We are continuing development of the EnterprisePlatform repository.

The repository has already been audited against the actual source code.

Use the existing audit document:

`EnterprisePlatform — Platform Capability & Developer Usage Manual`

as the current-state baseline.

## Objective

Execute **Phase 1 only**:

1. Make `Nucleus.SharedSchema` a real buildable project.
2. Correctly integrate `Nucleus.SharedSchema` into the solution.
3. Repair `APIPlatform.CrudEngine` so it compiles against the real platform contracts.
4. Repair `APIPlatform.Rbac` so it compiles.
5. Remove accidental dependency on temporary/stub projects where the real platform contracts should be used.
6. Preserve the existing architecture and design.
7. Do not implement new platform capabilities.
8. Do not redesign CrudEngine or RBAC.
9. Do not start Nucleus Builder.
10. Do not implement Grid, Search, SignalR, Workflow, Storage, Dashboard, etc.

The goal is:

> **Make the existing SharedSchema + CrudEngine + RBAC architecture buildable and internally coherent, without changing its intended design.**

---

# 1. FIRST — INSPECT BEFORE MODIFYING

Before changing anything, inspect:

```text
backend/EnterprisePlatform.sln

backend/src/APIPlatform.Foundation
backend/src/APIPlatform.Shared
backend/src/APIPlatform.CrudEngine
backend/src/APIPlatform.Authorization
backend/src/APIPlatform.Authorization/APIPlatform.Rbac

nucleus/shared/Nucleus.SharedSchema

backend/playground/APIPlatform.Playground

backend/tests
```

Also inspect all `.csproj` and project references.

Do not start editing immediately.

First determine the exact current dependency graph.

---

# 2. SHARED SCHEMA

Current audit finding:

`nucleus/shared/Nucleus.SharedSchema` contains the real metadata models/enums but has no `.csproj`, is not in the solution, and is therefore not a build artifact.

The real models include:

```text
EntityDefinition
FieldDefinition
RelationshipDefinition
ValidationRuleDefinition
UiHintDefinition
PermissionRequirement

FieldDataType
FieldSourceType
UiInputType
```

Inspect all of them before creating the project.

## Required action

Create the appropriate:

```text
nucleus/shared/Nucleus.SharedSchema/Nucleus.SharedSchema.csproj
```

using the repository's existing target framework and engineering conventions.

Do not invent a different framework.

Determine whether it should target:

```text
net10.0
```

based on the existing platform architecture.

Add the project to:

```text
backend/EnterprisePlatform.sln
```

or the appropriate solution structure if the repository has a more correct convention.

Do not duplicate the models.

The existing models are the source of truth.

---

# 3. CRUDENGINE PROJECT REFERENCE

Inspect:

```text
backend/src/APIPlatform.CrudEngine/APIPlatform.CrudEngine.csproj
```

The audit reports that its SharedSchema reference points to a path that does not exist:

```text
..\Nucleus.SharedSchema\Nucleus.SharedSchema.csproj
```

Correct this to the actual SharedSchema project location.

Do not copy SharedSchema into `backend/src`.

Do not create a second SharedSchema.

There must be exactly one canonical SharedSchema implementation.

Expected conceptual structure:

```text
EnterprisePlatform
│
├── backend
│   └── src
│       ├── APIPlatform.Foundation
│       ├── APIPlatform.Shared
│       ├── APIPlatform.Database
│       ├── APIPlatform.CrudEngine
│       └── ...
│
└── nucleus
    └── shared
        └── Nucleus.SharedSchema
```

Preserve this separation unless the actual repository architecture proves another location is required.

---

# 4. CRUDENGINE MISSING CONTRACTS

The audit identified references to:

```text
IEntityDefinitionProvider
```

that are not declared in the current repository.

Do NOT immediately create an interface just to make compilation pass.

First trace:

```text
IEntityDefinitionProvider
```

through the entire CrudEngine codebase.

Determine:

* why it is required
* where it is expected to come from
* whether the architecture already has an equivalent abstraction
* whether EntityDefinition itself is intended to be supplied directly
* whether the missing interface was part of an older architecture
* whether it should be replaced by an existing contract
* whether it genuinely needs to exist in Foundation

Use the simplest solution that preserves the current CrudEngine design.

### Important

Do NOT add an abstraction merely because the compiler reports a missing type.

Understand the dependency first.

If an interface is genuinely required by the existing architecture, place it in the correct generic platform boundary.

Do not place application-specific concepts into Foundation.

---

# 5. CRUDENGINE ARCHITECTURE MUST BE PRESERVED

The existing CrudEngine design must remain intact.

Inspect and preserve the existing concepts:

```text
ICrudEngine<TEntity>
CrudEngine<TEntity>

GenericRepository<TEntity>

EntityMetadataCache

CrudContext

QuerySqlBuilder
SqlQueryBuilder

FilterClauseBuilder
SortClauseBuilder
PagingClauseBuilder

SqlServerDialect
HanaDialect
```

Also preserve the existing pipeline:

```text
MetadataResolutionStage
        ↓
ContextEnrichmentStage
        ↓
ValidationStage
        ↓
ExecutionPlanningStage
        ↓
ExecutionStage
        ↓
ResponseMappingStage
```

Do not replace the architecture with a different CRUD implementation.

Do not introduce Entity Framework.

Do not introduce another ORM.

Do not introduce microservices.

Do not create application-specific repositories.

The purpose of this phase is to make the existing generic engine compile.

---

# 6. PROVIDER PORTABILITY

Preserve:

```text
SQL Server
SAP HANA
```

The existing architecture intentionally supports both.

Do not introduce SQL Server-specific behavior into shared abstractions.

Do not remove:

```text
SqlServerDialect
HanaDialect
```

Validate generated SQL according to the existing architecture.

Do not add PostgreSQL/MySQL/etc.

This phase is only about the currently supported providers.

---

# 7. CRUD VALIDATION

After fixing compilation:

Inspect every public CRUD operation:

```text
GetAsync
ListAsync
InsertAsync
UpdateAsync
DeleteAsync
```

Verify:

```text
metadata resolution
validation
SQL generation
parameter handling
mapping
sorting
filtering
pagination
transactions
provider dialect
```

Do not add new behavior unless the existing implementation clearly requires it to function.

If something is incomplete but not required for compilation, document it instead of expanding scope.

---

# 8. RBAC — FIRST FIX THE BUILD

Inspect:

```text
backend/src/APIPlatform.Authorization/APIPlatform.Rbac
```

The audit identified:

```text
FieldMaskDescriptor.cs
```

with:

```text
CS0120
```

related to:

```text
FieldMaskDescriptor.FieldAccess
```

Understand the intended model before fixing it.

Make the smallest production-grade correction.

Do not redesign:

```text
PermissionEvaluator
PermissionResolver
PolicyEngine
PermissionGrant
Field masking
Row filtering
```

---

# 9. RBAC STUB PROJECTS

The audit found:

```text
APIPlatform.Foundation.Stub
Nucleus.SharedSchema.Stub
```

inside the RBAC structure.

Inspect why they exist.

The real platform already contains:

```text
APIPlatform.Foundation
nucleus/shared/Nucleus.SharedSchema
```

Determine whether the RBAC project can now reference the real projects directly.

Preferred direction:

```text
APIPlatform.Rbac
       ↓
APIPlatform.Foundation
       ↓
real platform contracts
```

and:

```text
APIPlatform.Rbac
       ↓
Nucleus.SharedSchema
```

if SharedSchema is genuinely required.

Do not blindly replace the stubs.

Compare the type definitions first.

For each stub type determine:

```text
Stub type
Real type
Differences
Can direct replacement be done?
Does an adapter need to exist?
Should the stub be removed?
```

The goal is to eliminate accidental architectural duplication.

---

# 10. DO NOT BREAK THE RBAC DESIGN

Preserve the existing concepts:

```text
PermissionKey
PermissionEffect
Allow
Deny
Deny precedence

PolicyRule
PolicyEngine

PermissionEvaluator

Field masking

Row filtering

Permission evaluation pipeline
```

Do not add ASP.NET Core authorization integration in this phase.

That is Phase 2/3 work.

For this phase:

> RBAC only needs to compile and remain internally coherent.

Do not implement:

```text
IAuthorizationHandler
IAuthorizationRequirement
AddAuthorization
RequirePermission middleware
```

yet.

---

# 11. PROJECT/SOLUTION CLEANUP

After repairing the dependencies:

Ensure:

```text
Nucleus.SharedSchema
APIPlatform.CrudEngine
APIPlatform.Rbac
```

have valid project references.

Check:

```text
ProjectReference
TargetFramework
Nullable
ImplicitUsings
PackageReference
Namespace
AssemblyName
```

Do not introduce unnecessary package dependencies.

---

# 12. BUILD ORDER

Use incremental builds.

First:

```bash
dotnet build <Nucleus.SharedSchema>
```

Then:

```bash
dotnet build <APIPlatform.CrudEngine>
```

Then:

```bash
dotnet build <APIPlatform.Rbac>
```

Then:

```bash
dotnet build backend/EnterprisePlatform.sln
```

Fix errors caused by this phase only.

Do not start fixing unrelated empty modules.

---

# 13. TESTING

Run:

```bash
dotnet test backend/EnterprisePlatform.sln
```

Existing tests must continue passing.

Current audit baseline:

```text
96 tests
96 passed
0 failed
0 skipped
```

Do not reduce existing test coverage.

---

# 14. ADD TARGETED TESTS ONLY WHERE NECESSARY

Add tests only for the changes introduced in this phase.

At minimum consider:

### SharedSchema

Verify:

* project builds
* models compile
* enums compile

### CrudEngine

Verify:

* metadata resolution
* SQL Server SQL generation
* HANA SQL generation
* basic filter
* sorting
* paging

Use the existing test style.

Do not create a massive new test framework.

### RBAC

Verify:

* PermissionEvaluator still works
* allow
* deny
* deny precedence
* policy evaluation
* field mask behavior if existing tests/console harness support it

Again, keep tests focused.

---

# 15. DO NOT CREATE A REAL APPLICATION YET

Do NOT build:

```text
Employee
Customer
CRM
IQS
HRMS
Inventory
```

into the platform.

Do not put business entities into platform modules.

We will prove one entity end-to-end in the next phase.

---

# 16. DO NOT START UI YET

Do not modify:

```text
ui-platform-foundation
ui-platform-auth
ui-platform-forms
```

unless a build/reference issue is directly caused by this phase.

Do not start:

```text
Grid
Routing
SignalR
Workflow
Dashboard
Storage
```

They are intentionally outside this phase.

---

# 17. ARCHITECTURAL VALIDATION

After implementation, answer:

### SharedSchema

Is there now exactly one canonical SharedSchema?

### CrudEngine

Does CrudEngine consume the real SharedSchema?

### RBAC

Does RBAC consume the real platform contracts rather than temporary duplicate stubs?

### Dependency direction

Is dependency direction still:

```text
Application
    ↓
Platform modules
    ↓
Shared contracts
```

and not the reverse?

### Business boundary

Did any application-specific logic enter the platform?

The answer must be NO.

---

# 18. IMPORTANT — NO UNNECESSARY REFACTORING

Do not:

* rename large numbers of classes
* reorganize folders unnecessarily
* redesign interfaces
* rewrite working Database code
* rewrite Authentication
* rewrite Notification
* change provider architecture
* introduce new patterns
* introduce new frameworks
* create future modules
* implement speculative features

This is a **foundation repair**, not a rewrite.

---

# 19. FINAL VERIFICATION

At the end run:

```bash
dotnet build backend/EnterprisePlatform.sln
dotnet test backend/EnterprisePlatform.sln
```

If possible also build the individual projects.

Report:

```text
SharedSchema
    Build: PASS/FAIL

CrudEngine
    Build: PASS/FAIL

RBAC
    Build: PASS/FAIL

Full Solution
    Build: PASS/FAIL

Tests
    Passed:
    Failed:
    Skipped:

Warnings:
```

---

# 20. FINAL REPORT

Do not just say "done".

Produce a detailed implementation report containing:

## A. Changes Made

Every modified/created file.

```text
File
Change
Reason
```

## B. Dependency Changes

Show before/after.

Example:

```text
CrudEngine
    Before → broken SharedSchema path
    After  → real Nucleus.SharedSchema project
```

## C. SharedSchema

Explain:

* project created
* solution registration
* project references
* models preserved

## D. CrudEngine

Explain:

* build errors found
* root causes
* exact corrections
* existing architecture preserved

## E. RBAC

Explain:

* build error
* stub dependencies
* real dependency integration
* exact corrections
* architecture preserved

## F. Tests

Show test results.

## G. Remaining Issues

Clearly separate:

```text
Fixed in this phase

Still incomplete

Known limitations

Next phase
```

Do not hide unresolved issues.

---

# SUCCESS CRITERIA

This phase is successful only when:

```text
Nucleus.SharedSchema
        ↓
Buildable
        ↓
CrudEngine
        ↓
Buildable
```

and:

```text
APIPlatform.Foundation
        ↓
Nucleus.SharedSchema
        ↓
APIPlatform.Rbac
        ↓
Buildable
```

and:

```text
dotnet build backend/EnterprisePlatform.sln
        ↓
SUCCESS
```

and:

```text
dotnet test backend/EnterprisePlatform.sln
        ↓
ALL EXISTING TESTS PASS
```

Most importantly:

> **Do not claim CrudEngine or RBAC are production-ready merely because they compile.**

This phase only establishes:

**Buildable + internally coherent + correctly referenced.**

The next phase will prove actual runtime behavior.

---

# PHASE BOUNDARY

When this phase is complete, STOP.

Do not automatically proceed to the next phase.

The next planned phase is:

# Phase 2 — One Generic Entity End-to-End

That phase will prove:

```text
SharedSchema
      ↓
CrudEngine
      ↓
SQL Server
      ↓
API
      ↓
RBAC
      ↓
UIPlatform
      ↓
Form/Grid
```

But do not implement Phase 2 as part of this task.
