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
| [Make the tracking dashboard's live events match the hub](tracking-live-events-contract.md) | P2 | Open | admin-dashboard | The tracking view listens for `LocationUpdate` and `Notification` with payloads the hub does not send, and the server never sends either to the dashboard. |
| [Add the missing Payments table migration](payments-table.md) | P2 | Open | Api | `PaymentRepository` targets a `Payments` table no migration creates; add the migration and real-database tests. |
| [Build the Add Driver and Generate Invoice forms](admin-dashboard-quick-action-forms.md) | P2 | Open | admin-dashboard | The Quick Actions link to `?action=add` and `?action=generate`, but the Drivers and Billing pages have no such forms. |
| [Align the dashboard types with what the API sends](admin-dashboard-api-wire-types.md) | P2 | Open | admin-dashboard | Enums arrive as numbers and locations are nested; only job status is converted, priority and invoice status are not, and `Job` expects flat addresses. |
| [Decide whether admins can call the tracking write endpoints](admin-tracking-access.md) | P2 | Open | Api | Admins are not in the `ManagerOrDriver` policy, so start, stop, location and check-deviation answer 403 for them. |
| [De-duplicate payment webhook events](payment-webhook-deduplication.md) | P2 | Open | Api | Store processed provider event ids and skip repeats, so a replayed or redelivered event does not update a payment twice. |
| [Pass CancellationToken through driver endpoints and billing queries](driver-cancellation-tokens.md) | P3 | Open | Api | `DriversController` and two billing queries drop the token because the service and repository methods take none. |
| [Stop exposing entities and mutable shared DTOs from actions](entities-in-controller-actions.md) | P3 | Open | Api | Controllers still take and return entities and mutable `Domain.DTOs`; introduce `sealed record` HTTP DTOs. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | P3 | Open | admin-dashboard | Add Vitest and React Testing Library for frontend unit and component tests. |
| [Check why overdue jobs exclude Pending jobs](job-overdue-excludes-pending.md) | P3 | Open | Api | `GetOverdueJobsAsync` passes Pending as the cancelled status, so pending jobs are never overdue, unlike the `isOverdue` filter. |
