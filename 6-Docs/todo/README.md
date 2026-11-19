---
id: todo/index
title: Todos
doc-type: index
status: current
owner: 'unassigned'
last-reviewed: 2026-11-14
---

# Todos

Work identified but not done now — a finding, a deferred fix, a declined suggestion.
Capturing it here is what stops it evaporating between sessions.

Add one document per todo and list it here, ordered by priority (`P1` high, `P2` medium, `P3` low).

| Todo | Priority | Status | Component | Summary |
|---|---|---|---|---|
| [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md) | P1 | Open | admin-dashboard | D6 follow-up: SignalR event name mismatch, first-page Active Jobs count, stale `JobStatus` enum in `src/types/index.ts`. |
| [Complete Azure configuration support](azure-configuration-support.md) | P1 | Open | Api | Plan and verify local settings and Azure App Configuration with Key Vault references. |
| [Admin dashboard Playwright tests marked fixme](admin-dashboard-playwright-baseline.md) | P2 | Open | admin-dashboard | Eleven dashboard e2e tests check behaviour the app lacks: overview error/empty states, Quick Actions, live stats, SignalR injection. |
| [Add the missing Payments table migration](payments-table.md) | P2 | Open | Api | `PaymentRepository` targets a `Payments` table no migration creates; add the migration and real-database tests. |
| [Converge repository contracts on IRepository<T> and cancellation](repository-contract-convergence.md) | P3 | Open | Api | Bring the five domain-specific repository contracts under `IRepository<T>` and add cancellation tokens to its remaining members. |
| [Review unauthenticated endpoints](unauthenticated-endpoints.md) | P1 | Open | Api | `TrackingController` has no `[Authorize]`; payment webhooks are blocked by the class-level `[Authorize]`. |
| [Pass CancellationToken through driver endpoints and billing queries](driver-cancellation-tokens.md) | P3 | Open | Api | `DriversController` and two billing queries drop the token because the service and repository methods take none. |
| [Stop exposing entities and mutable shared DTOs from actions](entities-in-controller-actions.md) | P3 | Open | Api | Controllers still take and return entities and mutable `Domain.DTOs`; introduce `sealed record` HTTP DTOs. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | P3 | Open | admin-dashboard | Add Vitest and React Testing Library for frontend unit and component tests. |
