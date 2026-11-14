---
id: todos/generic-repository-migration
title: Migrate to the generic repository contract
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
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
