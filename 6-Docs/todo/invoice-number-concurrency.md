---
id: todos/invoice-number-concurrency
title: Make invoice number generation concurrency-safe
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Make invoice number generation concurrency-safe

`InvoiceRepository.GetNextInvoiceNumberAsync()` (`4-Persistence/HotshotLogistics.Data/Repositories/InvoiceRepository.cs:294-306`) computes the next `INV######` identifier using `SELECT ISNULL(MAX(...), 0) + 1` without a database sequence, lock, or transaction. Concurrent `GenerateInvoiceAsync` calls (or parallel test runs, as noted in the [test coding standard](../standards/test/test.standards.md#known-gaps)) can generate duplicate invoice numbers and collide on the unique constraint.

## Next action

Replace the unlocked `MAX() + 1` query with an atomic SQL Server `SEQUENCE` or lock/transaction-safe allocation in FluentMigrator and `InvoiceRepository`, remove the test file-lock workaround if no longer needed, and verify with concurrent integration tests.
