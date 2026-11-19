---
id: todos/job-status-update-delivery
title: Make the server deliver job status updates to the dashboard
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Make the server deliver job status updates to the dashboard

The dashboard overview now refreshes when the server sends `JobStatusUpdated`
([resolved overview todo](../archive/todo/admin-dashboard-job-status-unresolved.md)), but the server does not send it to
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

## Next action

Decide the delivery: send job status changes to an `admins` group that dashboard connections join on connect (checked
against the caller's role), call it from `JobService.UpdateJobStatusAsync`, and align the other event and method names in
`signalr.ts` and the tracking tests with `IRealtimeHubClient`. Cover it with a hub test that a status change reaches an
admin connection.
