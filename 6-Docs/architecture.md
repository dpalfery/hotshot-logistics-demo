---
id: architecture-layout
title: Clean Architecture folder layout
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-11-14
---

# Clean Architecture Guidelines

The Hotshot Logistics project follows Clean Architecture with numbered folders to enforce dependency direction. Layer placement rules also live in [file organization](rules/file-organization.md); this document is the human overview.

## Key Architectural Decisions

- Dependency injection throughout
- Repository pattern for data access (ADO.NET implementations)
- Application services for orchestration
- Inner layers never depend on outer layers
- Testable design with mocked dependencies
- Infrastructure is Terraform, not Pulumi

## File Placement Guidelines

When creating, moving, or adding files, place them in the appropriate numbered folder:

### 0-Base/
Foundational code and abstractions shared across layers. No dependencies on higher layers.

### 1-Presentation/
API (`HotshotLogistics.Api`) and admin dashboard (`admin-dashboard`). Presentation features depend on Application; the API composition root may also reference persistence, domain contracts, and base abstractions for startup and dependency-injection wiring.

### 2-Application/
Application services, validators, authorization handlers, and SignalR hubs. Depends on Domain and inward-facing shared contracts and base abstractions.

### 3-Domain/
Entities, value objects, contracts, repository interfaces. No dependencies on other layers.

### 4-Persistence/
Native ADO.NET repositories and FluentMigrator migrations. Implements Domain contracts.

### 5-Test/
Unit, integration, and architecture tests (xUnit, FluentAssertions).

### 6-Docs/
Canonical documentation. See the [documentation index](README.md).

### 7-Deployment/
CI/CD, Docker Compose, Terraform under `Azure-deploy/`.

## Search Guidelines

1. Search the most relevant numbered layer first
2. Expand along dependency direction if needed
3. Use folder prefixes (for example `2-Application/`)
4. For cross-cutting concerns, check `0-Base/` first

## Enforcement

- New files go in the correct folder at creation
- Moves must preserve layer direction
- Exceptions need explicit review
