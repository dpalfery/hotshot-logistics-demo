---
id: rules/file-organization
title: File Organization Rule
doc-type: rule
status: current
owner: unassigned
last-reviewed: 2026-11-14
---

# File Organization Rule

Enforces the numbered Clean Architecture folder layout when creating, moving, or searching files. See [architecture](../architecture.md) for the structural overview; technology-specific conventions live in `<csharp-coding-standard>`, `<data-access-layer-coding-standard>`, and `<test-coding-standard>`.

- **`0-Base/`** — Shared foundational abstractions, cross-cutting utilities, enums, and exceptions. No dependencies on higher layers.
- **`1-Presentation/`** — API (`HotshotLogistics.Api`) and Next.js dashboard (`admin-dashboard`). Depends on Application; the API composition root may reference Persistence, Domain contracts, and Core for startup wiring.
- **`2-Application/`** — Application services, workflows, authorization handlers, SignalR hubs, and validators. Depends on Domain, Contracts, and Core.
- **`3-Domain/`** — Domain entities, value objects, shared DTOs, and Contracts interfaces. No dependencies on outer layers.
- **`4-Persistence/`** — Persistence implementations and FluentMigrator migrations implementing Domain contracts.
- **`5-Test/`** — Unit, integration, and architecture tests, fixtures, and test helpers. Scoped exceptions: admin dashboard Playwright E2E specs live under `1-Presentation/admin-dashboard/tests/` and DbSetup CLI tests live under `7-Deployment/DbSetup/HotshotLogistics.DbSetup.Tests/`.
- **`6-Docs/`** — Canonical documentation, standards, rules, plans, specs, and todos (no scratch or ignored files).
- **`7-Deployment/`** — Terraform, Docker Compose, DbSetup, and deployment/dev-env scripts.
