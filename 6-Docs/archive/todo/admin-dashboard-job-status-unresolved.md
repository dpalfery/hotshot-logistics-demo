---
id: todos/admin-dashboard-job-status-unresolved
title: Resolve unresolved DashboardOverview issues
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Resolve unresolved DashboardOverview issues

The accepted `Admin dashboard job status cards` plan left three pre-existing issues unresolved in `DashboardOverview.tsx` and the frontend typing layer. They were explicitly deferred under D6 and still need a dedicated follow-up change.

## Resolution

Resolved 2026-11-19; the [admin dashboard architecture](../../admin-dashboard/architecture.md) states the resulting behaviour.

- The overview listens for `JobStatusUpdated`, the name the server sends (`IRealtimeHubClient`). The name is one constant,
  `JOB_STATUS_UPDATED_EVENT` in `src/services/signalr-events.ts`. An update invalidates every `jobs` query, so the
  totals and Recent Jobs refetch. `useRealtimeSubscription` owns the connection for the overview and the tracking page,
  and the overview says so when the connection fails.
- Active Jobs is counted by the server across every page: `GET /api/job?isActive=true&pageSize=1` filters on every status
  but `Received`, and the card reads `totalCount`. It is its own request, so it loads and fails independently.
- `JobStatus` in `src/types/index.ts` is the server's four values (`Pending`, `Assigned`, `EnRoute`, `Received`). The API
  sends them as numbers; `parseJobsPage` converts them, and a status the server does not define is an error state. The
  `InProgress`, `Completed` and `Cancelled` members, which the server never had, are gone from every component and spec.
- The remaining `fixme`, `should handle real-time statistics updates`, expected a `dashboard-update` window event and revenue
  from `/api/dashboard/stats`, neither of which exists. It is replaced by `should refresh the statistics and recent jobs
  when the server pushes a job status update`, which pushes `JobStatusUpdated` through the SignalR mock and passes.
  Revenue is not a feature of the overview, so that part of the old test is dropped.
- Not solved here, and tracked: the server sent `JobStatusUpdated` only to per-job groups and no production code sent it
  (since resolved: [job status update delivery](job-status-update-delivery.md)), and the other enums and the job shape
  differ from the wire ([dashboard wire types](../../todo/admin-dashboard-api-wire-types.md)).

## Decisions

| ID | Question | Answer | Status |
|---|---|---|---|
| D1 | Where does the durable record of this completed todo live? | The resolution in this archived todo is the record. | Approved |

## Closeout mapping

| ID | Destination |
|---|---|
| D1 | Documentation waiver — the resolution in this archived todo is the lasting record |

## Design harvest

Documentation waiver — nothing survives because the completed work is recorded in this archived todo and canonical docs were updated when the work landed.
