---
id: todo/index
title: Todos
doc-type: index
status: draft
owner: 'unassigned'
last-reviewed: 2026-08-17
---

# Todos

Work identified but not done now — a finding, a deferred fix, a declined suggestion.
Capturing it here is what stops it evaporating between sessions.

Add one document per todo and list it here.

| Todo | Status | Component | Summary |
|---|---|---|---|
| [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md) | Open | admin-dashboard | D6 follow-up: SignalR event name mismatch, first-page Active Jobs count, stale `JobStatus` enum in `src/types/index.ts`. |
| [Align C# contract model placement](contracts-model-placement.md) | Open | Api | Plan a migration of contract classes and shared DTOs to their prescribed locations. |
| [Complete Azure configuration support](azure-configuration-support.md) | Open | Api | Plan and verify local settings and Azure App Configuration with Key Vault references. |
| [Introduce a SQL connection factory](sql-connection-factory.md) | Open | Api | Plan a factory and migrate direct repository connection creation. |
| [Migrate to the generic repository contract](generic-repository-migration.md) | Open | Api | Design and adopt `IRepository<T>` while preserving repository behavior. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | Open | admin-dashboard | Add the unit and component test tooling specified by the test standard. |
| [Reconcile test locations with file organization rule](reconcile-test-locations.md) | Open | admin-dashboard | Reconcile Playwright and DbSetup test locations with the `5-Test` rule. |
