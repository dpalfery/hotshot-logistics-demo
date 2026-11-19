---
id: todos/tracking-live-events-contract
title: Make the tracking dashboard's live events match the hub
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Make the tracking dashboard's live events match the hub

[Job status delivery](../archive/todo/job-status-update-delivery.md) made `JobStatusUpdated` reach the dashboard. The
tracking view's other two live events still do not match the hub, and fixing only their names would hand its handlers
payloads they cannot read:

- The client listens for `LocationUpdate` with one object `{ jobId, driverId, location, timestamp }`. The hub sends
  `LocationUpdated(jobId, locationUpdate)` with a flat `latitude`, `longitude`, `speed`, `heading`, `accuracy` and no
  driver id, and only to the `job-{id}` group, which the dashboard does not join.
- The client listens for `Notification` with `{ id, type, title, message, timestamp, read }`. The hub sends
  `NotificationReceived(message)` where `type` is a number, there is no `read`, and a message with a `userId` goes only
  to that user.
- The tracking Playwright tests push the client's names, so they pass without proving the server contract.
- The hub's relay methods (`JobStatusUpdated`, `LocationUpdated`, `DriverStatusChanged`, `NewJobAvailable`,
  `NotificationReceived`) are public hub methods, so any authenticated client can invoke them and push to the `admins`
  group or all clients. `JoinJobTracking` lets any authenticated caller join any job's group.

## Next action

Decide what the tracking view shows and who may see it, then align the two events: the payload type, the audience (for
example send location updates to `admins` too), the TypeScript types and the tracking tests. Restrict the relay methods
and `JoinJobTracking` to the roles that need them (check the driver app first: a driver may invoke `LocationUpdated`).
