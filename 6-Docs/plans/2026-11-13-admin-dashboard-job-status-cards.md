---
id: plans/admin-dashboard-job-status-cards
title: Admin dashboard job status cards
doc-type: plan
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-13
development-mode: test-first
documentation-impact: extends-component
impact-components:
  - admin-dashboard
  - Api
---

# Admin dashboard job status cards

Status: Ready
Date: 2026-11-13
Goal: Add four job status cards (Pending, Assigned, En Route, Received) to the admin dashboard overview, backed by one server-side aggregate count endpoint, so the cards show true totals across all jobs and match the existing stats-card design.

## Problem and goal

The request, verbatim: "Update the Hotshot Logistics Admin Dashboard UI and backend to include four new job status cards, maintaining design consistency and enabling end-to-end data flow."

The overview page (`DashboardOverview.tsx`) shows four cards (Total Jobs, Active Jobs, Active Drivers, Overdue Invoices) and no per-status breakdown. The backend has no endpoint that returns counts per job status; the only status query returns full job lists (`GET api/job/by-status/{status}`) and the dashboard's `GET api/job` call is first-page only.

**Goal:** a new endpoint returns the count of jobs in each of the four backend statuses, and the dashboard renders four cards from it, styled exactly like the existing stats cards, with Playwright coverage and backend tests written first.

## Scale

- `documentation-impact: extends-component` — extends the existing `admin-dashboard` and `Api` catalog components; creates none.
- `impact-components`: `admin-dashboard`, `Api`.

## Investigation findings

Revision read: worktree `copilot/feat/admin-dashboard-job-status-cards` at `7c307dc` (base origin/development), via CodeGraph `projectPath` on the worktree plus narrow direct reads (listed under Discovery).

- **Status vocabulary (backend, authoritative):** `HotshotLogistics.Core.Enums.JobStatus` in `0-Base/HotshotLogistics.Core/Enums/JobStatus.cs` has exactly four values: `Pending`, `Assigned`, `EnRoute`, `Received`. The seed migrations use the same four. The request's "four new job status cards" maps one-to-one onto this enum.
- **Frontend vocabulary is stale:** `1-Presentation/admin-dashboard/src/types/index.ts` `JobStatus` is `Pending/Assigned/InProgress/Completed/Cancelled`, so the existing "Active Jobs" card compares against values the server never sends. This is the already-recorded todo `6-Docs/todo/admin-dashboard-job-status-unresolved.md` (stale enum, first-page Active Jobs, SignalR event-name mismatch). This plan does not fix it (D4) but must not depend on it: the new cards key off a new `JobStatusSummary` shape, never off the stale enum.
- **Existing card design:** `DashboardOverview.tsx` renders a `stats-container` grid (`grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6`) of `bg-white p-6 rounded-lg shadow` cards (`data-testid="stats-card"`), each with a `w-8 h-8 bg-<color>-500 rounded-md` letter badge and a `dt` (`text-sm font-medium text-gray-500 truncate`) and `dd` (`text-2xl font-semibold text-gray-900`, a `metric-*` testid) pair. Data uses react-query `useQuery` with `apiService`; the loading gate covers jobs, drivers and invoices.
- **Existing tests:** Playwright specs in `1-Presentation/admin-dashboard/tests/` (`dashboard-overview.spec.ts` mocks `https://localhost:5001/api/job` and others via `page.route`). Several assertions count `stats-card`, so the new cards must use a different `data-testid`.
- **Backend flow:** `JobController` (`1-Presentation/HotshotLogistics.Api/Controllers/JobController.cs`; ctor takes `IJobService`, `IJobRepository`, `ILogger`, `IValidator<Job>`) calls `IJobService`/`JobService` (`2-Application/HotshotLogistics.Application/Services/JobService.cs`; ctor takes `IJobRepository`, `ICustomerRepository`, `IDriverRepository`, `INotificationService`, `IMappingService`, `ILogger`), which calls `IJobRepository`/`JobRepository` (raw ADO.NET `SqlConnection`, table `Jobs`, `Status` stored as int). `by-status/{status}` uses the `ManagerOrAdmin` policy.
- **Serialization:** the API uses Newtonsoft with default camelCase and no string-enum converter, so enums serialize as integers. The new DTO therefore carries explicit integer properties and no enum keys on the wire.
- **DTO location:** DTOs live in `3-Domain/HotshotLogistics.Domain/DTOs` (e.g. `JobFilterDto`, `PagedResult`); there is no `Contracts.Models` folder in this tree. Contracts holds interfaces only.
- **Routing:** `GET api/job/status-summary` is a literal segment and wins over `GET api/job/{id}`.
- **Guard tests:** `HotshotLogistics.Tests.Utils.Infrastructure.ArchitectureTests` (NetArchTest) must stay green.
- **Discovery:** codegraph 1, docs 0 (the docs MCP tools are not exposed to this agent; the plan index, documentation index and the todo were read directly, self-gathered), direct reads 8: `JobStatus.cs`, `DashboardOverview.tsx`, `types/index.ts`, `dashboard-overview.spec.ts` (head), `Program.cs` (serialization), `JobRepository.GetJobsByStatusAsync`, `JobController` ctor, and archived plans for plan format. Reason: CodeGraph returned only trimmed excerpts for these files.

## Decisions

| ID | Question | Answer / decision | Mitigating and supporting information | Status / approval provenance |
|---|---|---|---|---|
| D1 | Which four statuses get cards? | One card per backend `JobStatus` value: Pending, Assigned, En Route, Received | The enum has exactly four values and the request says four; derived metrics such as Overdue or Unassigned would have no existing status model to rest on. | autonomous: recommendation accepted |
| D2 | Where are the counts computed? | Server side: one `GROUP BY Status` query behind a new `GET api/job/status-summary` endpoint | Client counting of `GET api/job` is first-page only (the known Active Jobs defect) and fetching four `by-status` lists transfers full rows. One aggregate call is accurate and cheap. | autonomous: recommendation accepted |
| D3 | Wire contract | `JobStatusSummaryDto` in `HotshotLogistics.Domain.DTOs` with int properties `Pending`, `Assigned`, `EnRoute`, `Received`; JSON `{pending, assigned, enRoute, received}`; all four always present (zero filled); authorized with `ManagerOrAdmin` | Integer properties avoid the enum-as-int wire ambiguity; zero fill keeps the UI free of missing-key handling; matches the `by-status` policy. | autonomous: recommendation accepted |
| D4 | Fix the stale frontend `JobStatus` enum, Active Jobs and SignalR name now? | No; out of scope, stays in the existing todo | Those are a separate recorded follow-up; coupling them widens blast radius and changes existing test expectations. | autonomous: recommendation accepted |
| D5 | Card refresh behaviour | react-query fetch on page load only; no SignalR live update | The SignalR path on this page is a known unresolved defect (todo); live update would build on it. | autonomous: recommendation accepted |
| D6 | Loading and error presentation | The status query is not part of the page's loading gate. While loading each value shows `…`; on error each shows `—`; the page and existing cards are unaffected | Existing specs mock only some routes; an unmocked or failing status call must not break the existing cards or tests. | autonomous: recommendation accepted |
| D7 | Card visual design | Reuse the existing card markup in a second grid (`data-testid="job-status-container"`, cards `data-testid="job-status-card"`); badge colors yellow/Pending `P`, blue/Assigned `A`, purple/En Route `E`, green/Received `R`; metric testids `metric-pending-jobs`, `metric-assigned-jobs`, `metric-en-route-jobs`, `metric-received-jobs` | Distinct testids keep existing `stats-card` counts intact; the same classes keep design consistency. | autonomous: recommendation accepted |
| D8 | Test strategy for the dashboard | Playwright spec (the repository's only dashboard test runner) with `page.route` mocks; no new test framework | Adding Vitest or similar would be a new dependency, which AGENTS.md reserves for user approval. | autonomous: recommendation accepted |
| D9 | Development mode | `test-first` | Repository default; no user opt-out supplied. | autonomous: recommendation accepted |
| D10 | Approve and execute gate | Approved: execute the plan as written | The conductor passed AUTONOMOUS_APPROVAL for D10 under autonomy `autonomous`. | autonomous: recommendation accepted |

## Design harvest

| Target component | Target doc type | Document | Claim to revisit at closeout |
|---|---|---|---|
| Api | architecture | `6-Docs/api/architecture.md` (Components and interfaces; Data models) | New `GET api/job/status-summary` endpoint, `JobStatusSummaryDto` shape, `IJobRepository.GetJobStatusCountsAsync` and `IJobService.GetJobStatusSummaryAsync` |
| admin-dashboard | architecture | `6-Docs/admin-dashboard/architecture.md` (Components and interfaces; Data models; Testing strategy) | Dashboard overview job status cards, `apiService.getJobStatusSummary`, `JobStatusSummary` type, the new Playwright spec |

## Test contract (development-mode: test-first)

Wire shape every task shares: `GET /api/job/status-summary` returns `200 {"pending":int,"assigned":int,"enRoute":int,"received":int}`.

| Task | Test project or file | Runner command | Observable behavior | RED evidence required | GREEN acceptance |
|---|---|---|---|---|---|
| T1 | No test surface — compile stubs (contract skeleton) | `dotnet build HotshotLogistics.sln --configuration Release` | DTO, interface members and throwing implementations compile so RED tests can compile | n/a | Release build passes |
| T2 (RED) | `5-Test/tests/HotshotLogistics.Tests/Jobs/`: `HotshotLogistics.Tests.Jobs.JobStatusSummaryServiceTests`, `HotshotLogistics.Tests.Jobs.JobControllerStatusSummaryTests`, `HotshotLogistics.Tests.Jobs.JobRepositoryStatusSummaryTests`, `HotshotLogistics.Tests.Jobs.JobStatusSummaryIntegrationTests` | `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~StatusSummary"` | Service zero-fills missing statuses, maps all four counts and ignores undefined status values; controller returns 200 with the DTO and 500 on service exception; repository groups counts by status (DB-conditional `DatabaseFact`); integration GET returns 200 with the four-property shape | Failing run (NotImplementedException from the T1 stubs) with only the new test files added | n/a |
| T3 (GREEN) | Same four classes, plus guards | Same filter command, plus the guard filters `FullyQualifiedName~ArchitectureTests` and `FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobControllerTests` run as two separate `dotnet test` commands (see T3) | Same | n/a | Both commands pass (DB-bound tests are skipped by `DatabaseFact` when no database is configured, and the skip is reported) |
| T4 (RED) | New `1-Presentation/admin-dashboard/tests/dashboard-job-status-cards.spec.ts` | `cd 1-Presentation/admin-dashboard && npx playwright test tests/dashboard-job-status-cards.spec.ts` | Four `job-status-card` elements render labelled Pending, Assigned, En Route, Received with mocked counts in the four `metric-*` testids; an error response shows `—` in all four while the existing four stats cards still render | Failing run on the unmodified UI | n/a |
| T5 (GREEN) | Same spec plus `dashboard-overview.spec.ts` | `cd 1-Presentation/admin-dashboard && npx playwright test tests/dashboard-job-status-cards.spec.ts tests/dashboard-overview.spec.ts` | Same; existing overview tests unchanged in result versus baseline | n/a | New spec passes; `dashboard-overview.spec.ts` has no new failures versus baseline; lint and build clean |
| T6 | No test surface — documentation gates | `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .` | Docs describe the endpoint and cards truthfully | n/a | Both checks pass for touched documents |

Guard classes to keep green: `HotshotLogistics.Tests.Utils.Infrastructure.ArchitectureTests`, `HotshotLogistics.Tests.Jobs.JobControllerTests`.

## Tasks

### T1 — Backend contract skeleton

- **Objective:** Add the DTO, interface members and throwing implementations so RED tests compile.
- **Session:** `status-backend-stubs`
- **Skills:** `csharp-dev`
- **Depends on:** none
- **Files:**
  - new `3-Domain/HotshotLogistics.Domain/DTOs/JobStatusSummaryDto.cs` (class `JobStatusSummaryDto`: int `Pending`, `Assigned`, `EnRoute`, `Received`; XML docs per StyleCop settings).
  - `3-Domain/HotshotLogistics.Contracts/Repositories/IJobRepository.cs` — add `GetJobStatusCountsAsync` returning `Task<IReadOnlyDictionary<JobStatus, int>>` with a `CancellationToken`.
  - `3-Domain/HotshotLogistics.Contracts/Services/IJobService.cs` — add `GetJobStatusSummaryAsync` returning `Task<JobStatusSummaryDto>` with a `CancellationToken`.
  - `4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs` — `GetJobStatusCountsAsync` stub throwing `NotImplementedException`.
  - `2-Application/HotshotLogistics.Application/Services/JobService.cs` — `GetJobStatusSummaryAsync` stub throwing `NotImplementedException`.
  - `1-Presentation/HotshotLogistics.Api/Controllers/JobController.cs` — action `GetJobStatusSummary` (`[HttpGet("status-summary")]`, `ManagerOrAdmin`, `ProducesResponseType` of `JobStatusSummaryDto` 200) calling the service; no other logic yet.
  - Any other `IJobRepository` or `IJobService` implementers or test fakes that fail to compile (discover with CodeGraph).
- **Command:** `dotnet build HotshotLogistics.sln --configuration Release`
- **Acceptance criteria:** the solution builds Release clean; existing `JobControllerTests` still compile; Contracts holds interfaces only; the DTO lives in Domain.

### T2 — Backend tests (RED)

- **Objective:** Write the four backend test classes in the Test contract and capture them failing against the T1 stubs.
- **Session:** `status-backend-red`
- **Skills:** `test-dev`
- **Depends on:** T1
- **Files:** new in `5-Test/tests/HotshotLogistics.Tests/Jobs/`: `JobStatusSummaryServiceTests.cs` (class `JobStatusSummaryServiceTests`; build `JobService` with Moq mocks of its six ctor dependencies), `JobControllerStatusSummaryTests.cs` (class `JobControllerStatusSummaryTests`; construct `JobController` as `JobControllerTests` does), `JobRepositoryStatusSummaryTests.cs` (class `JobRepositoryStatusSummaryTests`; mirror the `JobRepositoryTests` fixture, `DatabaseFact`), `JobStatusSummaryIntegrationTests.cs` (class `JobStatusSummaryIntegrationTests`; mirror `JobControllerIntegrationTests`, GET `/api/Job/status-summary`).
- **Command:** `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~StatusSummary"`
- **Acceptance criteria:** tests compile; the service and controller tests fail for the stated reason (not a compile error) against the unmodified tree plus the T1 stubs; output reported verbatim; no source edits and no git stash.

### T3 — Backend implementation (GREEN)

- **Objective:** Implement the count query, summary mapping and controller action.
- **Session:** `status-backend-green`
- **Skills:** `csharp-dev`
- **Depends on:** T2
- **Files:**
  - `4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs` — `GetJobStatusCountsAsync`: `SELECT Status, COUNT(*)` from `GetTableName()` `GROUP BY Status`, same connection pattern as `GetJobsByStatusAsync`.
  - `2-Application/HotshotLogistics.Application/Services/JobService.cs` — `GetJobStatusSummaryAsync`: zero-fill all four statuses, ignore undefined status values, return `JobStatusSummaryDto`.
  - `1-Presentation/HotshotLogistics.Api/Controllers/JobController.cs` — `GetJobStatusSummary`: 200 with the DTO, catch-all 500 with the controller's standard message and logging.
- **Commands:**
  - `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~StatusSummary"`
  - `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests"`
  - `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobControllerTests"`
- **Acceptance criteria:** both commands pass (DB-conditional tests reported as skipped when no database is configured); Release build clean; no secrets; no business rule in the controller.

### T4 — Dashboard tests (RED)

- **Objective:** Write the Playwright spec for the four cards and capture it failing.
- **Session:** `status-ui-red`
- **Skills:** `test-dev`
- **Depends on:** none
- **Files:** new `1-Presentation/admin-dashboard/tests/dashboard-job-status-cards.spec.ts` (follow `dashboard-overview.spec.ts` auth bypass and `page.route` mocking and `tests/utils/test-helpers.ts`; mock `https://localhost:5001/api/job/status-summary` to `{pending:5,assigned:3,enRoute:8,received:3}` plus the jobs, drivers and invoices routes the overview spec mocks; the error case returns 500 and expects `—`).
- **Command:** `cd 1-Presentation/admin-dashboard && npx playwright test tests/dashboard-job-status-cards.spec.ts`
- **Acceptance criteria:** the spec exists and fails against the unmodified UI because the cards are absent; failing output reported verbatim; no source edits.

### T5 — Dashboard implementation (GREEN)

- **Objective:** Add the type, API method and four cards.
- **Session:** `status-ui-green`
- **Skills:** `react-dev`
- **Depends on:** T4
- **Files:**
  - `1-Presentation/admin-dashboard/src/types/index.ts` — add `JobStatusSummary` (`pending`, `assigned`, `enRoute`, `received`, all number); leave the existing `JobStatus` enum untouched (D4).
  - `1-Presentation/admin-dashboard/src/services/api.ts` — `ApiService.getJobStatusSummary` requesting `/job/status-summary`.
  - `1-Presentation/admin-dashboard/src/components/dashboard/DashboardOverview.tsx` — `DashboardOverview`: a fifth `useQuery` (`['job-status-summary']`, `retry: false`) outside the loading gate (D6); a second grid after the Stats Cards per D7 reusing the existing card classes.
- **Commands:**
  - `cd 1-Presentation/admin-dashboard && npx playwright test tests/dashboard-job-status-cards.spec.ts tests/dashboard-overview.spec.ts`
  - `cd 1-Presentation/admin-dashboard && npm run lint && npm run build`
- **Acceptance criteria:** the new spec passes; `dashboard-overview.spec.ts` shows no new failures versus its baseline; lint and build clean; the `stats-card` count stays four.

### T6 — Documentation closeout

- **Objective:** Update the two harvest documents and the catalog review dates for the delivered behaviour.
- **Session:** `status-docs`
- **Skills:** `docs-dev`
- **Depends on:** T3, T5
- **Files:** `6-Docs/api/architecture.md` (Components and interfaces, Data models), `6-Docs/admin-dashboard/architecture.md` (Components and interfaces, Data models, Testing strategy), `6-Docs/catalog.md` (Last reviewed for the Api and admin-dashboard rows).
- **Command:** `kyber-weave docs validate . --merge-ready && kyber-weave docs drift .`
- **Acceptance criteria:** every harvest row updated truthfully; both checks pass for the touched documents.

## Dependency graph

```
T1 -> T2 -> T3 --\
                  >-> T6
T4 -> T5 --------/
```

The backend chain (T1–T3) and the dashboard chain (T4–T5) share no files and run in parallel in separate worktrees; T6 joins them. In the single compiled test project, T2 is RED then T3 GREEN, with T1 supplying the compile stubs.

## Risks

- DB-dependent repository and integration tests skip without a configured database; the worker reports that, and the service and controller tests carry the logic coverage.
- Route `status-summary` versus `{id}`: literal segments win; the integration test proves it.
- Existing overview specs do not mock the new route; D6 keeps them unaffected.
- Card counts can diverge from the stale Active Jobs card until the todo is done; documented and accepted (D4).

## Out-of-scope boundaries

- The stale frontend `JobStatus` enum, first-page Active Jobs count and SignalR event name (todo `admin-dashboard-job-status-unresolved`).
- Live SignalR updates of the cards, new statuses in the enum, migrations or seed data, infrastructure, deployment, dependencies, secrets.

## Verification gates

Each developer task ends with the worker's own task-gate run. Final: Release solution build, the Test-contract filters, the Playwright specs, dashboard lint and build, and both documentation checks.

## Review

The end-of-run council reviews the whole change under the `code-review` skill.

## docs-dev closeout

T6 performs the harvest into the documents above; docs-dev confirms the documentation-impact review at closeout.
