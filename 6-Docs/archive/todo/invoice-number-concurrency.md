---
id: todos/invoice-number-concurrency
title: Make invoice number generation concurrency-safe
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Make invoice number generation concurrency-safe

`InvoiceRepository.GetNextInvoiceNumberAsync()` (`4-Persistence/HotshotLogistics.Data/Repositories/InvoiceRepository.cs:294-306`) computes the next `INV######` identifier using `SELECT ISNULL(MAX(...), 0) + 1` without a database sequence, lock, or transaction. Concurrent `GenerateInvoiceAsync` calls (or parallel test runs, as noted in the [test coding standard](../../standards/test/README.md#known-gaps)) can generate duplicate invoice numbers and collide on the unique constraint.

## Next action

Replace the unlocked `MAX() + 1` query with an atomic SQL Server `SEQUENCE` or lock/transaction-safe allocation in FluentMigrator and `InvoiceRepository`, remove the test file-lock workaround if no longer needed, and verify with concurrent integration tests.

Resolved on 2026-11-19: invoice numbers now come from the one-row `InvoiceNumberCounters` table (migration `20261119000000`, seeded from the highest stored `INV######` number) advanced by a single atomic `UPDATE ... OUTPUT` in `GetNextInvoiceNumberAsync`. A `SEQUENCE` was not used because its start value is a DDL literal, which the SQL standard forbids building by concatenation. The test file lock is gone; concurrent-allocation and concurrent-`GenerateInvoiceAsync` tests prove unique numbers. Environments still running pre-change code allocate with `MAX() + 1`, so apply the migration and deploy together.

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
