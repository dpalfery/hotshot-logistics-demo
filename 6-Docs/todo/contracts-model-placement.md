---
id: todos/contracts-model-placement
title: Align C# contract model placement
doc-type: todo
status: current
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
