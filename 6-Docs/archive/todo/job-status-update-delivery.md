---
id: todos/job-status-update-delivery
title: Make the server deliver job status updates to the dashboard
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Make the server deliver job status updates to the dashboard

The dashboard overview now refreshes when the server sends `JobStatusUpdated`
([resolved overview todo](admin-dashboard-job-status-unresolved.md)), but the server does not send it to
the dashboard:

- `RealtimeService.BroadcastJobStatusUpdate` and `RealtimeHub.JobStatusUpdated` send only to the group `job-{jobId}`.
  Nothing joins the overview to a job group, and nothing joins any connection to the `admins` group that
  `DriverStatusChanged` already targets (`OnConnectedAsync` adds no group).
- No production code calls `IRealtimeService.BroadcastJobStatusUpdate`; only tests do. `JobService.UpdateJobStatusAsync`
  sends notifications but never the hub event.
- The other client handlers in `signalr.ts` do not match the server either: the client listens for `LocationUpdate`,
  `Notification` and `DriverStatusUpdate`, the server sends `LocationUpdated`, `NotificationReceived` and
  `DriverStatusChanged` with different arguments, and the client invokes `JoinJobGroup`, `LeaveJobGroup` and
  `SendLocationUpdate`, while the hub has `JoinJobTracking` and `LeaveJobTracking` only. The tracking dashboard's
  Playwright tests push the client's names, so they pass without proving the server contract.

## Resolution

Resolved 2026-11-19; the [API architecture](../../api/architecture.md#realtime-hub) and the
[admin dashboard architecture](../../admin-dashboard/architecture.md) state the resulting behaviour.

- `JobService` calls `IRealtimeService.BroadcastJobStatusUpdate` on every path that sets a job's status: create
  (`Pending`), `UpdateJobAsync` when the status changed, `AssignDriverAsync` (`Assigned`) and `UpdateJobStatusAsync`
  (start `EnRoute`, deliver and receive `Received`). The job has no cancelled status, and deleting a job removes it
  without a status, so neither sends the event.
- The event goes to the `admins` group and to the job's own `job-{id}` group, as `(jobId: string, status: number)`.
  The hub adds a connection to `admins` when it connects and the caller satisfies `ManagerOrAdmin`, so a reconnect is
  added again with no client call. The hub itself now requires an authenticated caller, and the dashboard sends its
  token (`accessTokenFactory`), as a header on negotiate and as `access_token` on the WebSocket.
- The client's names now match the hub where the arguments match too: `DriverStatusChanged`, `JoinJobTracking` and
  `LeaveJobTracking`, as constants in `signalr-events.ts`. The dead `sendLocationUpdate`, which invoked a method the hub
  does not have, is removed.
- Not solved here, and tracked in [tracking dashboard live events](../../todo/tracking-live-events-contract.md): the
  client's `LocationUpdate` and `Notification` events and their payloads still differ from the hub's `LocationUpdated`
  and `NotificationReceived`, and the server never sends either to the dashboard.
