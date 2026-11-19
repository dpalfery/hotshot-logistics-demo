---
id: todo/index
title: Todos
doc-type: index
status: current
owner: 'unassigned'
last-reviewed: 2026-11-19
---

# Todos

Work identified but not done now — a finding, a deferred fix, a declined suggestion.
Capturing it here is what stops it evaporating between sessions.

Add one document per todo and list it here, ordered by priority (`P1` high, `P2` medium, `P3` low).

| Todo | Priority | Status | Component | Summary |
|---|---|---|---|---|
| [Complete Azure configuration support](azure-configuration-support.md) | P1 | Open | Api | Plan and verify local settings and Azure App Configuration with Key Vault references. |
| [Make the server deliver job status updates to the dashboard](job-status-update-delivery.md) | P1 | Open | Api | `JobStatusUpdated` goes only to per-job groups nobody joins and no production code sends it; the client's other event and method names also differ from the hub. |
| [Add the missing Payments table migration](payments-table.md) | P2 | Open | Api | `PaymentRepository` targets a `Payments` table no migration creates; add the migration and real-database tests. |
| [Build the Add Driver and Generate Invoice forms](admin-dashboard-quick-action-forms.md) | P2 | Open | admin-dashboard | The Quick Actions link to `?action=add` and `?action=generate`, but the Drivers and Billing pages have no such forms. |
| [Align the dashboard types with what the API sends](admin-dashboard-api-wire-types.md) | P2 | Open | admin-dashboard | Enums arrive as numbers and locations are nested; only job status is converted, priority and invoice status are not, and `Job` expects flat addresses. |
| [Pass CancellationToken through driver endpoints and billing queries](driver-cancellation-tokens.md) | P3 | Open | Api | `DriversController` and two billing queries drop the token because the service and repository methods take none. |
| [Stop exposing entities and mutable shared DTOs from actions](entities-in-controller-actions.md) | P3 | Open | Api | Controllers still take and return entities and mutable `Domain.DTOs`; introduce `sealed record` HTTP DTOs. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | P3 | Open | admin-dashboard | Add Vitest and React Testing Library for frontend unit and component tests. |
| [Check why overdue jobs exclude Pending jobs](job-overdue-excludes-pending.md) | P3 | Open | Api | `GetOverdueJobsAsync` passes Pending as the cancelled status, so pending jobs are never overdue, unlike the `isOverdue` filter. |
