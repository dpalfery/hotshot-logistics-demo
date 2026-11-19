---
id: todos/generic-repository-migration
title: Migrate to the generic repository contract
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Migrate to the generic repository contract

The data-access standard calls for `IRepository<T>`, but the repository
currently uses domain-specific repository interfaces and contains separate
`IBaseRepository<T>` interfaces in Core and Contracts.

## Next action

Design and implement the `IRepository<T>` contract in Contracts. Review how
domain-specific interfaces and existing base repositories should use it, then
migrate implementations and consumers while preserving behavior. Cover the
changed contracts and repository behavior with focused tests.

Resolved on 2026-11-19: `IRepository<T>` is in `HotshotLogistics.Contracts`; the two duplicate `IBaseRepository<T>` interfaces are gone; `BaseRepository<T>` implements `IRepository<T>`; `ICustomerRepository` and `IPaymentRepository` extend it with unchanged signatures. The remaining contracts and cancellation tokens were done in [repository-contract-convergence](repository-contract-convergence.md).

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
