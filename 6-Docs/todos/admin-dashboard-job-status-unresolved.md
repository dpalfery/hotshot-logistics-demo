---
id: todos/admin-dashboard-job-status-unresolved
title: Resolve unresolved DashboardOverview issues
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-10-17
---

# Resolve unresolved DashboardOverview issues

The accepted `Admin dashboard job status cards` plan left three pre-existing issues unresolved in `DashboardOverview.tsx` and the frontend typing layer. They were explicitly deferred under D6 and still need a dedicated follow-up change.

## Deferred findings

- `DashboardOverview.tsx` currently listens for the SignalR event name `JobStatusUpdate`, but the server emits `JobStatusUpdated`.
- The Active Jobs value is still computed from the first page of jobs, so the count does not reflect the full result set.
- `src/types/index.ts` still carries a stale frontend `JobStatus` enum that does not match the backend's four-state summary values.

## Next action

Scope a separate follow-up that fixes the SignalR contract, computes Active Jobs from the full job dataset rather than the first page, and replaces the stale `JobStatus` enum with the canonical server-backed status values.
