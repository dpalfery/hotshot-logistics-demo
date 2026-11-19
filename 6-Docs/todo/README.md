---
id: todo/index
title: Todos
doc-type: index
status: current
owner: 'unassigned'
last-reviewed: 2026-11-14
---

# Todos

Work identified but not done now — a finding, a deferred fix, a declined suggestion.
Capturing it here is what stops it evaporating between sessions.

Add one document per todo and list it here, ordered by priority (`P1` high, `P2` medium, `P3` low).

| Todo | Priority | Status | Component | Summary |
|---|---|---|---|---|
| [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md) | P1 | Open | admin-dashboard | D6 follow-up: SignalR event name mismatch, first-page Active Jobs count, stale `JobStatus` enum in `src/types/index.ts`, and the one remaining fixme (overview live stats). |
| [Complete Azure configuration support](azure-configuration-support.md) | P1 | Open | Api | Plan and verify local settings and Azure App Configuration with Key Vault references. |
| [Standardize API ProblemDetails responses](problem-details.md) | P1 | Open | Api | Register `AddProblemDetails()` / `UseExceptionHandler()` and standardize error payloads. |
| [Add API health checks](health-checks.md) | P1 | Open | Api | Add `/health` dependency, readiness, and liveness checks. |
| [Make invoice number generation concurrency-safe](invoice-number-concurrency.md) | P2 | Open | Api | Replace unlocked `MAX() + 1` in `GetNextInvoiceNumberAsync` with a sequence or lock-safe allocation. |
| [Build the Add Driver and Generate Invoice forms](admin-dashboard-quick-action-forms.md) | P2 | Open | admin-dashboard | The Quick Actions link to `?action=add` and `?action=generate`, but the Drivers and Billing pages have no such forms. |
| [Define external-call resilience policies](resilience-policies.md) | P2 | Open | Api | Narrow `BillingService` retry exceptions and add Polly timeouts/circuit breakers on outbound HTTP clients. |
| [Define metrics and tracing](telemetry.md) | P2 | Open | Api | Configure OpenTelemetry metrics, distributed tracing, and Application Insights export. |
| [Add API rate limiting](rate-limiting.md) | P2 | Open | Api | Define global and endpoint rate limits and configure `AddRateLimiter()` / `UseRateLimiter()`. |
| [Remove Data dependency on Application](persistence-application-dependency.md) | P2 | Open | Api | Remove `HotshotLogistics.Data` project reference to `HotshotLogistics.Application`. |
| [Introduce a SQL connection factory](sql-connection-factory.md) | P2 | Open | Api | Plan a factory and migrate direct repository connection creation. |
| [Migrate to the generic repository contract](generic-repository-migration.md) | P2 | Open | Api | Design and adopt `IRepository<T>` while preserving repository behavior. |
| [Align C# contract model placement](contracts-model-placement.md) | P3 | Open | Api | Move the nine non-interface classes in `HotshotLogistics.Contracts` to `HotshotLogistics.Domain.DTOs`. |
| [Adopt sealed record HTTP DTOs](api-platform-hardening.md) | P3 | Open | Api | Migrate controller request/response DTOs to `sealed record` types. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | P3 | Open | admin-dashboard | Add Vitest and React Testing Library for frontend unit and component tests. |
