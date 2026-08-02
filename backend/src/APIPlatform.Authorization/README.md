# Nucleus — APIPlatform.Rbac

Implements the Rbac architecture reviewed previously, following
`docs/EnterprisePlatform_Execution_Standard.md` exactly (Permission Resolution → Context
Enrichment → Validation → Planning → Execution → Response Mapping).

## Projects
- `APIPlatform.Foundation.Stub` — STUB. Minimal ICurrentUser/ITenantContext/IEntityDefinition
  placeholders. Replace with the real frozen Foundation package when available.
- `Nucleus.SharedSchema.Stub` — STUB. Minimal entity/field metadata placeholder.
- `APIPlatform.Rbac` — the actual module. No dependency on Auth, Data, Eventing, or
  FeatureManagement (see code comments for why).
- `Nucleus.TestHarness.Rbac` — console smoke test proving one allow + one deny end-to-end.

## Run the smoke test
```
cd Nucleus.TestHarness.Rbac
dotnet run
```

## Known v1 limitations (flagged deliberately, not hidden)
- Role-level permission grant changes only invalidate cache for a directly-specified user, not
  every user holding that role (no user↔role index yet in the in-memory store).
- Row-level rule selection takes the first matching rule per entity; multi-rule composition is
  not implemented.
- `RequirePermissionAttribute` is declarative only — enforcement wiring belongs to a future
  Middleware/Host layer, intentionally not built here (would pull in an ASP.NET Core dependency).
