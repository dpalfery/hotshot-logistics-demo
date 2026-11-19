---
id: todos/repository-contract-convergence
title: Converge repository contracts on IRepository<T> and cancellation
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Converge repository contracts on IRepository<T> and cancellation

The Job, Driver, JobAssignment, Invoice and LocationTracking contracts did not extend `IRepository<T>` because
inheriting it would have added overloads that behave differently from their own members, and only `GetByIdAsync` took a
`CancellationToken`.

## Resolution

Resolved 2026-11-19; the [data access standard](../../standards/data-access-layer/README.md)
states the resulting contract.

- Every member of `IRepository<T>` takes a `CancellationToken`, and the services pass theirs to `AddAsync`,
  `UpdateAsync`, `GetAllAsync` and `DeleteAsync`.
- All seven repository contracts extend `IRepository<T>`. The duplicate members were folded into the generic ones:
  Invoice `GetByIdAsync(string)`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync(string)` and
  `ExistsAsync(string)`; JobAssignment `GetByIdAsync(string)`, `GetAllAsync`, `UpdateAsync` and `DeleteAsync(string)`;
  LocationTracking `AddAsync`, `GetByIdAsync(long)` and `ExistsAsync(long)`.
- `BaseRepository<T>` members are virtual. `InvoiceRepository` overrides get-by-id and add so a get or add through
  `IRepository<Invoice>` still loads and saves the line items. `JobAssignmentRepository` overrides get-by-id and
  get-all for its joined query. `JobRepository` and `DriverRepository` override add (and `DriverRepository` update) to
  stamp `CreatedAt` and `UpdatedAt`, so the generic members cannot write an unstamped row.
- Domain-specific members (`GetJobByIdAsync`, `CreateDriverAsync`, `CreateAsync` and the rest) are unchanged.
- Tests: `RepositoryContractTests` (contracts extend the generic one; every member takes a token),
  `RepositoryConnectionTests.GenericOperation_WithCancellationToken_PassesTokenToFactory` (all seven repositories by six
  members), `InvoiceRepositoryTests.GenericContract_AddAndGetById_SavesAndLoadsLineItems`.

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
