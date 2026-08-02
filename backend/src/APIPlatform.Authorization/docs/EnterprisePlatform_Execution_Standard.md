# EnterprisePlatform Execution Standard (v1)

**Status:** Official convention, effective for all modules from `APIPlatform.Rbac` onward.
**Applies retroactively (naming only, no code change) to:** `APIPlatform.CrudEngine`, `APIPlatform.Auth` — both already follow this shape; this document formalizes it.
**Does not modify:** `APIPlatform.Foundation`, `Nucleus.SharedSchema`, `APIPlatform.Data`, `APIPlatform.CrudEngine`, `APIPlatform.Auth`. These remain Version 1 Frozen. This is a documentation artifact only — no source in those packages is touched.

---

## 1. Purpose

Every EnterprisePlatform module — API or UI, present or future — processes a request through the same seven-stage lifecycle. The stage *names* are fixed; only the first stage's label adapts to the module's domain vocabulary. This gives every future module (Rbac, Workflow, Notification, UI modules) a predictable internal shape without forcing a shared base class or coupling unrelated packages together.

This is a **convention**, not a shared runtime dependency. A module conforms to this standard by structuring its own pipeline this way — it does not require referencing a common `APIPlatform.Pipeline` package unless one is later justified by real duplication (consistent with the platform's "don't introduce shared infra before at least two real consumers need it" pattern already used for Eventing).

## 2. The Seven Stages

```
Request
   │
   ▼
[Domain] Resolution Stage
   │
   ▼
Context Enrichment Stage
   │
   ▼
Validation Stage
   │
   ▼
Planning Stage
   │
   ▼
Execution Stage
   │
   ▼
Response Mapping Stage
   │
   ▼
Response
```

| Stage | Responsibility | Must NOT do |
|---|---|---|
| **[Domain] Resolution** | Understand the request; resolve everything needed before execution (entity metadata, identity, permissions, workflow definition, notification template, route) | Execute business logic |
| **Context Enrichment** | Populate execution context — current user, tenant, company, branch, JWT claims, session, correlation ID, defaults, configuration | Validate anything |
| **Validation** | Structural and metadata validation only (does the resource/action/shape make sense) | Execute operations, apply business rules |
| **Planning** | Decide *how* execution will occur — strategy, transaction scope, provider, SQL vs. stored proc, batching, pipeline ordering, dependency order | Execute anything |
| **Execution** | Do the actual work | Anything else |
| **Response Mapping** | Convert execution results into framework response models | Contain execution logic |

## 3. Stage-Name Table by Module

| Module | First Stage Name |
|---|---|
| `APIPlatform.CrudEngine` | Metadata Resolution Stage |
| `APIPlatform.Auth` | Identity Resolution Stage |
| `APIPlatform.Rbac` | Permission Resolution Stage |
| `APIPlatform.Workflow` | Workflow Resolution Stage |
| `APIPlatform.Notification` | Notification Resolution Stage |
| UIPlatform modules | Route Resolution Stage |

Stages 2–7 keep identical names across every module. Only the first stage's label changes, and only its label — its responsibility (resolve everything needed pre-execution) is identical everywhere.

## 4. Why This Shape

- **Predictability across a 10+ year, many-module platform.** A developer who understands one module's pipeline understands the shape of every other module's pipeline immediately — the domain content differs, the skeleton never does.
- **Clean separation between "what's needed" (Resolution/Enrichment), "is it valid" (Validation), "how" (Planning), and "doing it" (Execution).** This is what already made CrudEngine and Auth easy to extend without touching generated/core code — this document just names the pattern so it's applied deliberately rather than incidentally.
- **No shared base class required.** Each module implements its own stages in its own types. Conformance is structural, not inherited — keeping every package independently usable and minimal-dependency (per platform Hard Rule 3).

## 5. Conformance Checklist for a New Module

A module is considered standard-compliant when:
- [ ] Its first stage is named `<Domain> Resolution Stage`.
- [ ] Stages 2–7 use the exact names in Section 2.
- [ ] No business logic appears in Resolution or Context Enrichment.
- [ ] No execution occurs before the Execution stage.
- [ ] Response Mapping contains no execution logic — mapping only.
- [ ] Extension points (hooks, partials, named-delegate registries) are documented per the platform's existing Section 7 pattern, not embedded ad hoc inside a stage.

## 6. Scope Boundary of This Document

This document standardizes **stage naming and responsibility boundaries only**. It does not:
- Introduce a shared pipeline/middleware package (no such package exists or is proposed here).
- Redesign or touch Foundation, SharedSchema, Data, CrudEngine, or Auth internals.
- Define Rbac's actual pipeline implementation — that was covered in the prior Rbac architecture review and will be built against this standard in the next session.
