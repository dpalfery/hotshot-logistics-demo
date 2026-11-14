---
id: todos/contracts-model-placement
title: Align C# contract model placement
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Align C# contract model placement

The C# standard requires `Contracts` to contain interfaces only and shared DTOs
to live in `Contracts.Models`. The current `HotshotLogistics.Contracts` project
contains nine classes alongside interfaces:

- `Repositories/IInvoiceRepository.cs`: `InvoiceFilter`, `AgingReportEntry`, and
  `InvoiceSummary`.
- `Services/INotificationService.cs`: `NotificationPreferences`.
- `Services/IPaymentProcessor.cs`: `PaymentMethodDetails`,
  `PaymentProcessingResult`, `WebhookEventData`, `WebhookProcessingResult`, and
  `PaymentStatusUpdate`.

There is no tracked `Contracts.Models` project, `HotshotLogistics.Contracts.csproj`
currently references `HotshotLogistics.Domain.csproj`, and Domain also contains
DTOs whose intended sharing and placement need review.

## Next action

Plan a separate migration to align contract and shared DTO placement with the
C# standard. Review each affected type and its consumers, approve any new
project and dependency changes, and preserve behavior with focused tests.
