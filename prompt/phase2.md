# EnterprisePlatform — Phase 2: One Generic Entity End-to-End

We are continuing EnterprisePlatform after successful completion of Phase 1.

Phase 1 is complete:

```text
Nucleus.SharedSchema     BUILD PASS
APIPlatform.CrudEngine   BUILD PASS
APIPlatform.Rbac         BUILD PASS
Full Solution            BUILD PASS
Tests                     141 PASS
```

The Phase 1 implementation report is the current baseline.

## Phase 2 Objective

Prove that the repaired platform can support **one real generic business entity end-to-end** without putting business-specific code inside the platform.

The goal is NOT to build a complete application.

The goal is to prove this architecture:

```text
Test Application
      │
      ├── Entity Metadata
      │
      ▼
Nucleus.SharedSchema
      │
      ▼
APIPlatform.CrudEngine
      │
      ▼
APIPlatform.Database
      │
      ▼
SQL Server
      │
      ▲
      │
API Endpoint
      │
      ▼
RBAC Evaluation
      │
      ▼
UIPlatform Foundation
      │
      ▼
UIPlatform Forms
```

The final result must demonstrate that a generic entity can actually be:

```text
Created
Read
Listed
Updated
Deleted
Filtered
Sorted
Paginated
Validated
Protected by authorization
Rendered by the UI form engine
```

where those capabilities are actually supported by the current platform.

---

# IMPORTANT SCOPE RULES

This is a validation/integration phase.

Do NOT turn this into a general platform rewrite.

Do NOT implement:

```text
Grid
Search module
SignalR
Workflow
Storage
Dashboard
Reporting
Scheduler
AI
SAP integration
Notification delivery
Nucleus Builder
Plugin marketplace
Microservices
```

Those are future phases.

Do NOT redesign:

```text
CrudEngine
SharedSchema
RBAC
Database
Authentication
UIPlatform
```

unless an actual integration blocker requires a minimal correction.

Prefer fixing integration problems over redesigning abstractions.

---

# 1. FIRST — INSPECT CURRENT REPOSITORY

Before modifying anything, inspect:

```text
backend/EnterprisePlatform.sln

backend/src/APIPlatform.Foundation
backend/src/APIPlatform.Shared
backend/src/APIPlatform.Database
backend/src/APIPlatform.CrudEngine
backend/src/APIPlatform.Authorization
backend/src/APIPlatform.Authentication
backend/src/APIPlatform.Validation
backend/src/APIPlatform.Configuration
backend/src/APIPlatform.Logging

nucleus/shared/Nucleus.SharedSchema

backend/playground/APIPlatform.Playground

frontend/packages/ui-platform-foundation
frontend/packages/ui-platform-auth
frontend/packages/ui-platform-forms
```

Also inspect the Phase 1 changes.

Do not assume the Phase 1 report is sufficient.

Verify the actual code.

---

# 2. CHOOSE THE TEST APPLICATION LOCATION

Use the existing Playground/sample architecture if appropriate.

Prefer creating or extending a dedicated application validation host rather than putting an Employee entity into APIPlatform itself.

The entity must belong to the application/test host.

For example:

```text
backend/playground/APIPlatform.Playground
```

or a clearly separated sample application if the existing repository structure makes that more appropriate.

Do NOT place:

```text
Employee
EmployeeRepository
EmployeeController
Employee business rules
```

inside:

```text
APIPlatform.Foundation
APIPlatform.CrudEngine
APIPlatform.Database
APIPlatform.Rbac
Nucleus.SharedSchema
```

The platform must remain generic.

---

# 3. TEST ENTITY

Use exactly ONE simple entity.

Recommended:

```text
Employee
```

Keep it intentionally small.

Suggested fields:

```text
Id
EmployeeCode
Name
Email
Department
IsActive
CreatedOn
ModifiedOn
```

Do not add unnecessary business behavior.

The purpose is to test platform infrastructure, not Employee Management.

---

# 4. DATABASE TABLE

Create a minimal database table for the test entity using the existing database/migration architecture.

Follow the existing SQL Server migration conventions.

Do NOT use:

```text
IDENTITY
NEWID()
GETDATE()
SQL Server-only defaults
```

where that would violate the existing EnterprisePlatform portability principles.

IDs and timestamps should follow the existing platform/application conventions.

The schema must remain compatible with the platform's SQL Server/SAP HANA portability model wherever applicable.

If this phase is explicitly scoped to SQL Server runtime validation, use SQL Server as the runtime provider but do not pollute shared platform abstractions with SQL Server-specific logic.

---

# 5. SHARED SCHEMA METADATA

Define the Employee metadata using the real:

```text
Nucleus.SharedSchema
```

models.

Use:

```text
EntityDefinition
FieldDefinition
RelationshipDefinition
ValidationRuleDefinition
UiHintDefinition
PermissionRequirement
```

only where appropriate.

Do NOT create another metadata model.

Do NOT create:

```text
EmployeeMetadata
CustomEntityDefinition
CrudEntityDefinition
```

inside the platform.

The test application should provide the entity definition through the existing:

```text
IEntityDefinitionProvider
```

introduced during Phase 1.

---

# 6. ENTITY METADATA PROVIDER

Implement an application-level:

```text
IEntityDefinitionProvider
```

implementation.

For example:

```text
EmployeeEntityDefinitionProvider
```

or an application-level provider containing the Employee definition.

The important boundary is:

```text
Application
    ↓
IEntityDefinitionProvider
    ↓
EntityDefinition
    ↓
CrudEngine
```

The CrudEngine must NOT contain Employee-specific knowledge.

---

# 7. ENTITY MODEL

Create the Employee POCO in the test application.

It should satisfy whatever entity contract the existing CrudEngine requires.

Do not modify `IEntity` merely to accommodate Employee.

Do not add Employee properties to platform interfaces.

---

# 8. DATABASE REGISTRATION

Use the existing:

```text
APIPlatform.Database
```

abstraction.

Do not create another database abstraction.

Use:

```text
IDatabaseExecutor
```

and the existing provider registration.

For this phase validate SQL Server runtime behavior.

Use the existing provider registration pattern.

---

# 9. CRUD ENGINE REGISTRATION

Use the existing:

```text
AddCrudEngine()
```

or the actual Phase 1 registration mechanism.

Inspect the implementation first.

Register:

```text
IEntityDefinitionProvider
```

at the application level.

Do not create a NoOp metadata provider.

If metadata is missing, the application should fail clearly rather than silently generating incorrect SQL.

---

# 10. CRUD OPERATIONS TO PROVE

Create a minimal API surface in the test application.

Do NOT create a new generic CRUD framework.

The controller should simply demonstrate consumption of the existing CrudEngine.

Prove:

## Get

```text
GET /api/employees/{id}
```

## List

```text
GET /api/employees
```

## Insert

```text
POST /api/employees
```

## Update

```text
PUT /api/employees/{id}
```

## Delete

```text
DELETE /api/employees/{id}
```

If the CrudEngine's actual API uses different method shapes, follow the actual implementation rather than forcing these exact signatures.

The HTTP layer belongs to the application.

The generic CRUD behavior belongs to CrudEngine.

---

# 11. CRUD REQUEST FLOW

For every operation verify:

```text
HTTP
 ↓
Application Controller
 ↓
ICrudEngine<Employee>
 ↓
CrudEngine<Employee>
 ↓
Metadata Resolution
 ↓
Context Enrichment
 ↓
Validation
 ↓
Execution Planning
 ↓
Generic Repository
 ↓
IDatabaseExecutor
 ↓
Dapper
 ↓
SQL Server
```

Trace the actual code.

Do not simply test that an HTTP response is 200.

Verify the platform path is actually being used.

---

# 12. SQL GENERATION

Verify that SQL is generated from:

```text
EntityDefinition
+
Crud operation
+
filters
+
sorting
+
paging
```

Do not hand-write Employee SQL for the CRUD operations if the purpose is to prove generic CRUD.

If Employee-specific SQL appears in the CRUD controller/repository, treat that as a failure of this phase.

---

# 13. CREATE TEST

POST an Employee.

Verify:

```text
Id generated correctly
EmployeeCode persisted
Name persisted
Email persisted
Department persisted
IsActive persisted
timestamps handled correctly
```

Verify the database actually contains the row.

---

# 14. GET TEST

Retrieve the created Employee.

Verify:

```text
Correct entity
Correct fields
Correct mapping
```

---

# 15. LIST TEST

List Employees.

Verify:

```text
Multiple rows
Mapping
Pagination
```

if pagination is part of the existing CrudEngine API.

Do not implement a new pagination system.

---

# 16. FILTER TEST

Use the CrudEngine's existing filter mechanism.

At minimum prove an existing supported filter such as:

```text
EmployeeCode = ...
```

or another equality filter supported by the current implementation.

Do not implement contains/range/etc. unless the current CrudEngine already supports them.

The purpose is to prove existing functionality, not expand it.

---

# 17. SORT TEST

Use the existing sorting mechanism.

For example:

```text
Name ascending
```

or whatever the current API supports.

Verify the generated SQL and returned ordering.

---

# 18. PAGINATION TEST

Use the existing paging mechanism.

Verify:

```text
Page
PageSize
```

and returned results.

Validate the dialect being used is SQL Server.

Do not redesign pagination.

---

# 19. UPDATE TEST

Update an existing Employee.

Verify:

```text
Database row changes
ModifiedOn changes according to application/platform convention
```

Verify no unrelated fields are accidentally overwritten.

---

# 20. DELETE TEST

Delete an Employee.

Follow the actual platform semantics.

If CrudEngine implements physical delete, prove that.

If it implements soft-delete semantics, prove that.

Do not invent soft delete for this phase.

---

# 21. VALIDATION

Use the existing metadata validation pipeline.

Define at least one meaningful metadata-level validation rule if the current system supports it without modifying the platform.

For example:

```text
Name required
Email required
```

But do NOT implement business-specific Employee validation inside CrudEngine.

The important distinction is:

```text
Platform
    generic validation infrastructure

Application
    actual validation rules
```

---

# 22. RBAC — USE THE EXISTING ENGINE

Phase 1 only made RBAC buildable and internally coherent.

Now prove it can be used by the application.

However:

## DO NOT implement full ASP.NET Core authorization integration yet unless it is absolutely required by the existing API path.

First determine whether the existing RBAC API can be called directly from the application boundary without modifying its architecture.

Create simple permissions such as:

```text
employee.read
employee.create
employee.update
employee.delete
```

Use the existing PermissionKey/PermissionGrant model.

Prove at minimum:

```text
Allowed user → read succeeds

Denied user → read denied

Allowed user → update succeeds

Denied user → update denied
```

If the existing RBAC architecture requires ASP.NET Core policy integration to make this meaningful, document the blocker rather than creating a large authorization subsystem.

Do not redesign RBAC in this phase.

---

# 23. IMPORTANT — SHARED SCHEMA / RBAC PERMISSION MISMATCH

Phase 1 intentionally left:

```text
Nucleus.SharedSchema.Stub
```

inside RBAC because:

```text
FieldMetadata.DefaultPermissionKey
```

does not map directly to:

```text
FieldDefinition.Permissions
    ReadRoles
    WriteRoles
```

Do NOT invent a mapping during this phase.

For Employee-level authorization, use the existing RBAC permission-key model at the resource/action level.

Document the field-level permission mismatch as a known architectural decision still pending.

---

# 24. AUTHENTICATION

Use the existing APIPlatform.Authentication implementation.

Do not redesign authentication.

If the Playground's current hardcoded identity resolver is used, clearly label it as:

```text
TEST ONLY
```

Do not make the platform depend on a hardcoded employee user.

The objective is to establish:

```text
Authenticated request
        ↓
Current user
        ↓
RBAC context
        ↓
CRUD operation
```

If the existing refresh-token mismatch blocks this test, fix only the minimum contract required for the test and document the change.

Do not turn this into the full authentication modernization phase.

---

# 25. UI PLATFORM — FIRST REAL CONSUMER

After proving API CRUD, create the smallest possible React test application/page that actually consumes:

```text
ui-platform-foundation
ui-platform-auth
ui-platform-forms
```

This is the first real UIPlatform consumption test.

Do NOT implement Grid.

Do NOT implement Routing Platform.

Do NOT implement SignalR.

Do NOT implement Dashboard.

---

# 26. UI FOUNDATION

Use the actual:

```text
AppProvider
API client
TanStack Query wrappers
Zustand store
```

where appropriate.

Do not duplicate axios configuration in the application if the platform already provides it.

---

# 27. UI AUTH

Use:

```text
AuthProvider
useAuth
AuthGuard
```

if the current API contract can support them.

First inspect the known mismatches from the audit:

```text
refresh path
refresh request body
logout endpoint
```

Do not silently ignore them.

If they prevent the test from working, fix the smallest correct contract.

Document every change.

---

# 28. UI FORM

Use the existing:

```text
ui-platform-forms
```

metadata-driven form engine.

Create an Employee form from the same conceptual metadata.

At minimum:

```text
EmployeeCode
Name
Email
Department
IsActive
```

Use the existing field registry.

Use the existing Zod validation mechanism.

Do not create a separate form framework.

---

# 29. FORM → API

The Employee form must perform:

```text
Submit
 ↓
API client
 ↓
POST / employee
 ↓
CrudEngine
 ↓
Database
```

Then test edit:

```text
Form
 ↓
PUT
 ↓
CrudEngine
 ↓
Database
```

This is critical because it proves the UI form is not merely rendering.

---

# 30. DO NOT BUILD A GRID

There is currently no UI Grid platform.

For listing employees during this phase, use the simplest temporary application-level table/list if required.

Clearly label it:

```text
Application test UI
NOT UIPlatform Grid
```

Do not add a Grid package implementation.

Grid will be a later phase.

---

# 31. END-TO-END TEST

The final proof should be:

```text
Login
 ↓
Authenticated UI
 ↓
Employee Form
 ↓
POST
 ↓
API Controller
 ↓
CrudEngine
 ↓
SharedSchema
 ↓
Dapper
 ↓
SQL Server
 ↓
Response
 ↓
TanStack Query / UI state
 ↓
Rendered result
```

Then:

```text
Edit
 ↓
PUT
 ↓
CrudEngine
 ↓
Database
```

And:

```text
Delete
 ↓
DELETE
 ↓
CrudEngine
 ↓
Database
```

---

# 32. TESTING REQUIREMENTS

Add automated tests where practical.

At minimum:

## Backend

Test:

```text
Create
Get
List
Update
Delete
Filter
Sort
Paging
Validation
```

and the relevant RBAC behavior.

## Integration

Prefer at least one real integration test against SQL Server if the repository/environment makes this safely possible.

If a real SQL Server integration test cannot be executed, do not fake it and call it end-to-end.

Clearly report:

```text
Unit tested
Integration tested
Runtime manually tested
Not tested
```

---

# 33. DO NOT CLAIM SUCCESS FROM MOCKS ALONE

This is critical.

A test using:

```text
Fake IDatabaseExecutor
```

proves business/platform orchestration only.

It does NOT prove:

```text
CrudEngine → Dapper → SQL Server
```

actually works.

Separate:

```text
Unit
Integration
End-to-End
```

in the final report.

---

# 34. PERFORMANCE

Do not perform benchmarking in this phase.

Only verify obvious correctness:

```text
No unbounded query accidentally introduced
Paging is used where appropriate
Connections are disposed correctly
Transactions are correctly scoped
```

Do not optimize prematurely.

---

# 35. PORTABILITY

Runtime validation may use SQL Server.

However, verify that generic CRUD SQL generation still produces valid dialect-specific SQL for:

```text
SQL Server
SAP HANA
```

Use existing unit tests for HANA generation.

Do not require a live HANA database unless one is already safely available.

Do not modify the provider abstraction.

---

# 36. NO PLATFORM POLLUTION

Before finishing, inspect all changed files.

Confirm:

```text
Employee-specific code exists only in test/application project.

No Employee class in APIPlatform.

No Employee controller in CrudEngine.

No Employee permission hardcoded into RBAC.

No Employee UI component inside ui-platform-forms.

No business logic inside SharedSchema.
```

This is a mandatory architectural check.

---

# 37. ACCEPTANCE CRITERIA

Phase 2 is successful only if we can demonstrate:

### Backend

```text
[ ] SharedSchema provides Employee metadata
[ ] Application supplies IEntityDefinitionProvider
[ ] CrudEngine resolves Employee metadata
[ ] Employee CREATE works
[ ] Employee GET works
[ ] Employee LIST works
[ ] Employee UPDATE works
[ ] Employee DELETE works
[ ] Supported filtering works
[ ] Supported sorting works
[ ] Supported paging works
[ ] Validation works
[ ] SQL Server persistence works
[ ] RBAC allow works
[ ] RBAC deny works
```

### Frontend

```text
[ ] UIPlatform foundation builds
[ ] UIPlatform auth builds
[ ] UIPlatform forms builds
[ ] Real application mounts AppProvider
[ ] Real application mounts AuthProvider
[ ] Employee form renders using UIPlatform Forms
[ ] Form validation works
[ ] Form submits to real API
[ ] Create works through real API
[ ] Edit works through real API
[ ] Authentication is actually exercised
```

### Architecture

```text
[ ] No application logic added to platform
[ ] No duplicate CRUD engine created
[ ] No duplicate database abstraction created
[ ] No duplicate form framework created
[ ] No new ORM
[ ] No new microservice architecture
[ ] No speculative modules
```

---

# 38. FINAL VERIFICATION

Run backend:

```bash
dotnet build backend/EnterprisePlatform.sln
dotnet test backend/EnterprisePlatform.sln
```

Build/test frontend using the repository's actual package-manager conventions.

Do not modify the repository merely to make the package manager work unless that is part of the integration fix.

If frontend infrastructure is currently not buildable because of the known empty workspace configuration, fix only what is necessary to establish the test application.

---

# 39. FINAL REPORT

Create a detailed Phase 2 implementation report.

Use this structure:

# Phase 2 — One Generic Entity End-to-End: Implementation Report

## Final Verification

```text
Backend Build:
Backend Tests:
Frontend Build:
Frontend Tests:
Runtime:
Database:
```

## A. Test Application

Explain:

```text
Location
Purpose
Dependencies
```

## B. Employee Entity

Show:

```text
Entity
Fields
Database table
Metadata
```

## C. SharedSchema Integration

Explain exactly how metadata flows.

## D. CrudEngine Integration

Explain:

```text
IEntityDefinitionProvider
CrudEngine<TEntity>
GenericRepository
SQL generation
```

## E. Database Runtime

Explain actual SQL Server execution.

## F. API

List the actual endpoints created.

## G. Authentication

Explain actual authentication flow.

## H. RBAC

Show:

```text
Permission
User
Allowed operation
Denied operation
```

## I. UIPlatform

Explain:

```text
Foundation
Auth
Forms
API client
State/query
```

## J. End-to-End Flow

Provide an actual flow diagram:

```text
UI
 ↓
UIPlatform
 ↓
HTTP
 ↓
API
 ↓
RBAC
 ↓
CrudEngine
 ↓
SharedSchema
 ↓
Database
```

## K. Tests

Separate:

```text
Unit
Integration
End-to-End
Manual
```

## L. Problems Found

Document every issue.

## M. Changes Made

Every file changed/created.

## N. Remaining Issues

Do not hide anything.

## O. Phase 3 Recommendation

Recommend the next phase based on actual findings.

Do not automatically implement Phase 3.

---

# 40. FINAL STOP CONDITION

When the acceptance criteria are satisfied:

STOP.

Do not automatically start:

```text
Grid
Routing
Search
SignalR
Workflow
Storage
Dashboard
Nucleus Builder
```

Phase 2 exists to answer one question:

> **Can EnterprisePlatform actually build and run one generic enterprise entity end-to-end using its own platform abstractions?**

If the answer is YES, provide evidence.

If the answer is NO, clearly identify the exact blocker.

Do not hide failures behind mocks, placeholder implementations, or claims based only on compilation.

The objective is **proof of the platform architecture**, not maximum feature count.
