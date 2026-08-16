# API Platform — Master Engineering Context

Build and maintain a **production-grade Enterprise API Platform** that becomes the reusable foundation for IQS, Nucleus, CRM, Project, HRMS, Ticketing, and future applications.

## Core Goal
Keep **business/domain logic inside applications**. Build common technical capabilities once in the platform and reuse them across applications.

## Platform
- `APIPlatform.Database` — Dapper-based, provider-agnostic database layer; SQL Server + SAP HANA.
- `APIPlatform.Notification` — reusable notification engine.
- `APIPlatform.Realtime` — SignalR/realtime infrastructure.
- `APIPlatform.Audit` — reusable audit capability.
- `APIPlatform.CrossCutting` — orchestration/composition only; **no business or feature logic**.
- Future capabilities must remain independently usable modules/plugins.

## Engineering Principles
**Quality first.** Prefer correctness, simplicity, DRY, maintainability, scalability, performance, and long-term stability over speed or cleverness.

- Follow SOLID, clean architecture, separation of concerns, and dependency inversion.
- Reuse existing abstractions before creating new ones.
- Never duplicate logic when a reusable abstraction is appropriate.
- Keep modules loosely coupled and independently testable.
- Avoid premature abstraction and unnecessary complexity.
- Do not introduce microservices/HTTP communication between platform modules without a strong architectural reason.
- Preserve working behavior and backward compatibility unless a change is intentionally required.
- Keep database/provider/platform code application-agnostic.
- Design for SQL Server + SAP HANA portability.
- Never hide errors, silently fail, or use unsafe fallbacks.
- Validate edge cases, concurrency, failure paths, and resource lifetimes.
- Prefer async, cancellation support, proper disposal, and safe logging.
- Do not expose secrets or sensitive data in logs.
- Before changing architecture, inspect the existing code and understand dependencies.
- After changes, build, test, and verify affected consumers.

## Decision Rule
When multiple solutions work, choose the **simplest production-grade solution that remains extensible without over-engineering**.

## Final Outcome
Applications should consume platform capabilities directly or through CrossCutting orchestration while remaining focused on their own business/domain responsibilities. The platform must remain reusable, scalable, testable, maintainable, and reliable for the long term.