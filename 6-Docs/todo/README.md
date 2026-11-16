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
| [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md) | P1 | Open | admin-dashboard | D6 follow-up: SignalR event name mismatch, first-page Active Jobs count, stale `JobStatus` enum in `src/types/index.ts`. |
| [Complete Azure configuration support](azure-configuration-support.md) | P1 | Open | Api | Plan and verify local settings and Azure App Configuration with Key Vault references. |
| [Standardize API ProblemDetails responses](problem-details.md) | P1 | Open | Api | Register `AddProblemDetails()` / `UseExceptionHandler()` and standardize error payloads. |
| [Add API health checks](health-checks.md) | P1 | Open | Api | Add `/health` dependency, readiness, and liveness checks. |
| [Make invoice number generation concurrency-safe](invoice-number-concurrency.md) | P2 | Open | Api | Replace unlocked `MAX() + 1` in `GetNextInvoiceNumberAsync` with a sequence or lock-safe allocation. |
| [Admin dashboard Playwright tests marked fixme](admin-dashboard-playwright-baseline.md) | P2 | Open | admin-dashboard | Eleven dashboard e2e tests check behaviour the app lacks: overview error/empty states, Quick Actions, live stats, SignalR injection. |
| [Define external-call resilience policies](resilience-policies.md) | P2 | Open | Api | Narrow `BillingService` retry exceptions and add Polly timeouts/circuit breakers on outbound HTTP clients. |
| [Define metrics and tracing](telemetry.md) | P2 | Open | Api | Configure OpenTelemetry metrics, distributed tracing, and Application Insights export. |
| [Add API rate limiting](rate-limiting.md) | P2 | Open | Api | Define global and endpoint rate limits and configure `AddRateLimiter()` / `UseRateLimiter()`. |
| [Remove Data dependency on Application](persistence-application-dependency.md) | P2 | Open | Api | Remove `HotshotLogistics.Data` project reference to `HotshotLogistics.Application`. |
| [Introduce a SQL connection factory](sql-connection-factory.md) | P2 | Open | Api | Plan a factory and migrate direct repository connection creation, including `JobRepository` methods sanctioned by D7. |
| [Migrate to the generic repository contract](generic-repository-migration.md) | P2 | Open | Api | Design and adopt `IRepository<T>` while preserving repository behavior. |
| [Align C# contract model placement](contracts-model-placement.md) | P2 | Open | Api | Plan a migration of contract classes, project references, and shared Domain DTOs (`JobFilterDto`, `PagedResult`, `JobStatusSummaryDto`) to `Contracts.Models`. |
| [Reconcile the test standard with the opt-in live Playwright test](playwright-live-test-standard.md) | P2 | Open | admin-dashboard | Test standard forbids real-API Playwright tests; `job-status-cards.spec.ts` has an opt-in `@live` test (D14). |
| [Fix MigrationRunner MSB3539 intermediate output warning](migrationrunner-intermediate-output-path.md) | P3 | Open | Api | `BaseIntermediateOutputPath` set in the project body of `MigrationRunner.csproj` triggers MSB3539 on every build. |
| [Stop ESLint warning on CSS files in changed-file lint runs](eslint-css-file-ignored-warning.md) | P3 | Open | admin-dashboard | ESLint has no CSS config, so linting `globals.css` warns "File ignored" and CSS goes unchecked. |
| [Adopt sealed record HTTP DTOs](api-platform-hardening.md) | P3 | Open | Api | Migrate controller request/response DTOs to `sealed record` types. |
| [Add frontend unit-test tooling](frontend-unit-test-tooling.md) | P3 | Open | admin-dashboard | Add Vitest and React Testing Library for frontend unit and component tests. |
