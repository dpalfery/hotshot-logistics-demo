---
id: todos/admin-dashboard-job-status-unresolved
title: Resolve unresolved DashboardOverview issues
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Resolve unresolved DashboardOverview issues

The accepted `Admin dashboard job status cards` plan left three pre-existing issues unresolved in `DashboardOverview.tsx` and the frontend typing layer. They were explicitly deferred under D6 and still need a dedicated follow-up change.

## Deferred findings

- `DashboardOverview.tsx` currently listens for the SignalR event name `JobStatusUpdate`, but the server emits `JobStatusUpdated`.
- The Active Jobs value is still computed from the first page of jobs, so the count does not reflect the full result set.
- `tests/dashboard-overview.spec.ts` keeps one `test.fixme`, `should handle real-time statistics updates`. It expects the overview to refetch on a `dashboard-update` window event and to show revenue from `/api/dashboard/stats`, which the API does not have. A live refresh on the overview needs the SignalR event contract above, so decide that first, then rewrite the test against it. Its `getByText('1')` is also ambiguous and must be narrowed.
- `src/types/index.ts` still carries a stale frontend `JobStatus` enum that does not match the backend's four-state summary values.

## Next action

Scope a separate follow-up that fixes the SignalR contract, computes Active Jobs from the full job dataset rather than the first page, and replaces the stale `JobStatus` enum with the canonical server-backed status values.
