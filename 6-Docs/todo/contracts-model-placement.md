---
id: todos/contracts-model-placement
title: Align C# contract model placement
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
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

## Shared DTOs in Domain

The C# model-placement rule puts shared DTOs in `HotshotLogistics.Contracts.Models`,
not in the Domain project. These DTOs live in `3-Domain/HotshotLogistics.Domain/DTOs/`
(namespace `HotshotLogistics.Domain.DTOs`) and are shared across Contracts,
Application, Data and Api:

- `JobFilterDto.cs` (line 8): pre-existing; used by `IJobRepository`,
  `JobRepository.BuildWhereClause` and `JobService`.
- `PagedResult`: pre-existing.
- `JobStatusSummaryDto.cs`: added by the job-status dashboard plan under its
  decision D5, which placed it beside `JobFilterDto` because the standard's
  target project does not exist yet.

This matters because Domain is meant to hold entities and business rules;
transport DTOs there couple every consumer to Domain and keep the
`Contracts -> Domain` reference alive. Each new shared DTO added under the D5
exception grows the migration.

## Next action

Plan a separate migration to align contract and shared DTO placement with the
C# standard. Review each affected type and its consumers, approve any new
project and dependency changes, and preserve behavior with focused tests.
