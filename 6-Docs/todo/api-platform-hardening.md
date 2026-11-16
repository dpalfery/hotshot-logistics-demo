---
id: todos/api-platform-hardening
title: Adopt sealed record HTTP DTOs
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Adopt sealed record HTTP DTOs

The [C# coding standard](../standards/csharp/README.md#stack) targets `sealed record` request and response DTOs for controller actions. Currently, `0` `record` types exist across `0-Base/`–`4-Persistence/`.

## Controller actions that bind or return domain entities

The same standard says an action must never return a domain entity or persistence row. Several existing actions in `1-Presentation/HotshotLogistics.Api/Controllers/` do, and some also bind an entity as the request body. That exposes the domain model as the HTTP contract, so a change to an entity changes the API, and a client can set fields such as `Id` or `CreatedAt` directly.

- `JobController.cs`: `GetJobs` (`PagedResult<Job>`), `GetJobById`, `CreateJob` and `UpdateJob` (bind and return `Job`), `AssignDriver`, `UpdateJobStatus`, `GetJobsByStatus`, `GetJobsByDriver`, `GetJobsByCustomer`, `GetOverdueJobs`.
- `CustomerController.cs`: `GetCustomers`, `GetCustomerById`, `CreateCustomer` and `UpdateCustomer` (bind and return `Customer`), `GetActiveCustomers`, `GetOverdueCustomers`, `GetCustomerJobs` (`Job`), `GetCustomerInvoices` (`Invoice`).
- `BillingController.cs`: `GenerateInvoice`, `GetInvoice`, `GetCustomerInvoices`, `GetOverdueInvoices` (`Invoice`).
- `UserProfileController.cs`: `GetCurrentUserProfile` and the update action (bind and return `UserProfile`).
- `TrackingController.cs`: actions returning `LocationTracking`; confirm whether it is a domain entity during planning.

The job status summary action added by plan `2026-11-16-admin-dashboard-job-status-cards` already returns `JobStatusCountDto` and is not affected.

## Next action

Plan migrating HTTP request and response DTOs to `sealed record` types (coordinated with [contract model placement](contracts-model-placement.md)), replacing every entity-typed action parameter and return value listed above with a DTO mapped at the controller or application boundary. Preserve JSON serialization and existing controller contract tests, and update the admin dashboard types in `src/types/index.ts` if any response shape changes.
