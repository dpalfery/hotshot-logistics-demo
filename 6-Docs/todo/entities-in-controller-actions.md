---
id: todos/entities-in-controller-actions
title: Stop exposing entities and mutable shared DTOs from controller actions
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Stop exposing entities and mutable shared DTOs from controller actions

The [C# coding standard](../standards/csharp/csharp.standards.md#stack) says never to return a domain entity or persistence
row from an action and to use `sealed record` request and response bodies. The controller-defined types are done (see
[sealed record DTOs](../archive/todo/api-platform-hardening.md)), but:

- `JobController`, `CustomerController`, `BillingController` and `TrackingController` still accept or return the `Job`,
  `Customer`, `Invoice`, `LocationTracking` and `UserProfile` entities, and `PagedResult<Job>`.
- `DriverDto`, `JobAssignmentDto` and `PagedResult<T>` in `Domain.DTOs` are shared with services and repositories that set
  their properties, so they stay mutable classes until those callers construct them with `init` or a constructor.

Changing either alters the JSON the dashboard reads, so it needs an owner decision on the response shapes.

**Next action:** Decide the response shapes per endpoint, add wire-shape tests for the current JSON first, then introduce
the HTTP DTOs and map at the controller boundary.
