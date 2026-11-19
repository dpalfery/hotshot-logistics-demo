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

The [C# coding standard](../standards/csharp/README.md#async) requires a `CancellationToken` on I/O, including
controller actions. `DriversController` has none, because `IDriverService`, `DriverService` and the driver repository take
no token. `BillingService.GetCustomerInvoicesAsync` and `GetOverdueInvoicesAsync` also drop the token they receive, because
the invoice repository methods they call take none.

Fixing it changes the service and repository signatures, which belong to the persistence work. The generic
`IRepository<T>` members (get by id, get all, add, update, delete, exists) take a token on every repository now
([repository contract convergence](../archive/todo/repository-contract-convergence.md)), so the driver add, update and
get-all calls can pass one. The driver-specific repository methods already take a token, and `IDriverService` and
`DriversController` still do not.

**Next action:** Add an optional `CancellationToken` to the driver service methods and the two invoice repository
queries (`GetByCustomerIdAsync`, `GetOverdueInvoicesAsync`), pass it from the actions, and cover cancellation with a test.
