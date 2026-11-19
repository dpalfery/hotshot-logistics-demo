---
id: todos/admin-tracking-access
title: Decide whether admins can call the tracking write endpoints
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Decide whether admins can call the tracking write endpoints

`TrackingController` start, stop, location update and check-deviation need the `ManagerOrDriver` policy
(`RequireRole("Manager", "Driver")`, `Program.cs`), which does not include `Admin`. An admin identity therefore gets 403 on
those four actions, while the same admin can read location and history (`ManagerOrAdmin`). The integration tests' default
test identity is an admin, so they send `X-Test-Role: Manager` for these calls. Whether an admin should be able to start
or stop tracking and post locations is an access decision, not a code fix.

## Next action

The owner decides: add `Admin` to the policy used by those four actions (and extend `TrackingAuthorizationTests` and the
[API architecture](../api/architecture.md#authorization) to match), or keep them manager and driver only and document why.
