---
id: todos/contracts-model-placement
title: Align C# contract model placement
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Align C# contract model placement

The C# standard requires `Contracts` to contain interfaces only and shared DTOs
to live in `HotshotLogistics.Domain.DTOs` (owner decision, 2026-11-19). The current `HotshotLogistics.Contracts` project
contains nine classes alongside interfaces:

- `Repositories/IInvoiceRepository.cs`: `InvoiceFilter`, `AgingReportEntry`, and
  `InvoiceSummary`.
- `Services/INotificationService.cs`: `NotificationPreferences`.
- `Services/IPaymentProcessor.cs`: `PaymentMethodDetails`,
  `PaymentProcessingResult`, `WebhookEventData`, `WebhookProcessingResult`, and
  `PaymentStatusUpdate`.

These nine classes move to `HotshotLogistics.Domain.DTOs`. The DTOs already in
`Domain/DTOs` are in the right place.

## Next action

Plan a separate migration to align contract and shared DTO placement with the
C# standard. Review each affected type and its consumers, approve any new
project and dependency changes, and preserve behavior with focused tests.

Resolved on 2026-11-19: the nine classes moved to `HotshotLogistics.Domain.DTOs` (one type per file, names unchanged) and every consumer was updated. `ArchitectureTests.Contracts_should_contain_interfaces_only` keeps `HotshotLogistics.Contracts` interface-only.

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
