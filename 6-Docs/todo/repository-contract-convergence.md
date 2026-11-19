---
id: todos/repository-contract-convergence
title: Converge repository contracts on IRepository<T> and cancellation
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Converge repository contracts on IRepository<T> and cancellation

`IRepository<T>` now exists and `ICustomerRepository` and `IPaymentRepository` extend it. The
Job, Driver, JobAssignment, Invoice and LocationTracking contracts do not, because inheriting it
would add overloads that behave differently from their own members (Invoice `GetByIdAsync(object)`
would skip the line items that `GetByIdAsync(string)` loads; JobAssignment `GetAllAsync()` would
sit beside `GetAllAsync(CancellationToken)`).

`IRepository<T>` also keeps the pre-existing signatures: only `GetByIdAsync` takes a
`CancellationToken`. Adding an optional token to `AddAsync`, `UpdateAsync`, `GetAllAsync`,
`DeleteAsync` and `ExistsAsync` breaks the existing Moq expression trees
(`Verify(r => r.AddAsync(It.IsAny<Payment>()))`), so it needs those tests updated.

## Next action

Decide per contract: rename or fold the divergent members so the five remaining contracts can
extend `IRepository<T>`, and add the `CancellationToken` parameter to the remaining generic
members, updating the affected tests and callers in the same change.
