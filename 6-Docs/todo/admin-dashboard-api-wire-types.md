---
id: todos/admin-dashboard-api-wire-types
title: Align the dashboard types with what the API sends
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Align the dashboard types with what the API sends

The API writes enums as numbers (Newtonsoft default; `GET /api/job` returned `"status":0,"priority":2`) and nests
locations (`pickupLocation.address`). The dashboard's `src/types/index.ts` models its own shapes:

- `JobStatus` is converted from the number at the service boundary (`parseJobsPage`), the one conversion done so far.
- `JobPriority` has `Normal`, `High` and `Urgent` as strings; the server's `JobPriority` is `Low`, `Normal`, `Medium`,
  `High` and arrives as a number, so `priority` is neither a string nor a known member.
- `InvoiceStatus` is a string enum, while the server's invoice status arrives as a number.
- `Job` expects flat `pickupAddress` and `dropoffAddress`, X  show blank addresses against the real API.

The Playwright mocks use the client's shapes, so they cannot show the difference.

## Next action

Decide the wire contract once: either serialise enums as strings in the API (a breaking change for every client) or
convert each enum at the service boundary as job status is now, then give `Job` the API's real shape and make the mocks
match it.
