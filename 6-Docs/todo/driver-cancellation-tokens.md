---
id: todos/driver-cancellation-tokens
title: Pass CancellationToken through the driver endpoints and billing queries
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Pass CancellationToken through the driver endpoints and billing queries

The [C# coding standard](../standards/csharp/csharp.standards.md#async) requires a `CancellationToken` on I/O, including
controller actions. `DriversController` has none, because `IDriverService`, `DriverService` and the driver repository take
no token. `BillingService.GetCustomerInvoicesAsync` and `GetOverdueInvoicesAsync` also drop the token they receive, because
the invoice repository methods they call take none.

Fixing it changes the service and repository signatures, which belong to the persistence work.

**Next action:** Add an optional `CancellationToken` to the driver service and repository methods and the two invoice
repository queries, pass it from the actions, and cover cancellation with a test.
