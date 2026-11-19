---
id: todos/payments-table
title: Add the missing Payments table migration
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Add the missing Payments table migration

`PaymentRepository` reads and writes the `Payments` table, but no FluentMigrator migration creates
it, so every payment operation fails with "Invalid object name 'Payments'" against a database built
from the migrations. `BillingService` payment processing and `PaymentIntegrationTests` only pass
because they mock `IPaymentRepository`. Found while adding a schema check for the repository
column lists (`EntityColumnsTests`), which therefore leaves the Payment list out.

## Next action

Get the approved table-definition contract for `Payments` (columns are in
`EntityColumns.Payment`), add the migration, add a real-database `PaymentRepositoryTests`
class, and add `Payments` to `EntityColumnsTests`.
