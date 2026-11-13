---
id: todos/admin-dashboard-playwright-baseline
title: Admin dashboard Playwright tests marked fixme
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-12
---

# Admin dashboard Playwright tests marked fixme

The dashboard Playwright suite (`1-Presentation/admin-dashboard/tests/`) now passes on every configured browser. Eleven tests are `test.fixme` because the app does not implement the behaviour they check. Each needs an app change, then its `fixme` removed.

## Dashboard overview (`tests/dashboard-overview.spec.ts`)

- `should handle error state for statistics`: `DashboardOverview` has no statistics error state; query errors are logged and the cards show 0.
- `should handle empty recent jobs list`: the Recent Jobs list is hardcoded markup, not driven by the jobs API, so it has no empty state.
- `should navigate to create job form`, `should navigate to add driver form`, `should navigate to invoice generation`: the Quick Actions buttons have no click handler, and the Jobs, Drivers and Billing pages do not read `?action=`.
- `should handle real-time statistics updates`: the overview does not react to a `dashboard-update` event or show revenue. Related to the SignalR event-name mismatch in [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md).

## Tracking dashboard (`tests/tracking-dashboard.spec.ts`)

Five tests inject SignalR updates through `window.mockSignalRService`, which the app never reads (`TrackingDashboard` always uses the real client): `should connect to SignalR and handle real-time updates`, `should display location updates with proper formatting`, `should display notifications with read/unread status`, `should limit location updates and notifications to last 10 items`, `should disconnect SignalR when component unmounts`.

## Next action

Decide per item whether to build the behaviour or delete the test, and for the SignalR group choose a test seam in `signalr.ts` or a WebSocket-level mock.
