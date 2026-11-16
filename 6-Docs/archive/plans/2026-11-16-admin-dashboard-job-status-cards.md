---
id: plans/2026-11-16-admin-dashboard-job-status-cards
title: Admin dashboard job status cards
doc-type: plan
status: archived
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
development-mode: test-first
autonomy: autonomous
documentation-impact: extends-component
impact-components:
  - admin-dashboard
  - Api
---

# Admin dashboard job status cards

**Status: Archived, 2026-11-16.**

Date: 2026-11-16 · Branch: `copilot/feat/job-status-cards` · Base: `main` @ `ff86948`

## Problem and goal

The Admin Dashboard home (`DashboardOverview`) shows four metric cubes (Total Jobs,
Active Jobs, Active Drivers, Overdue Invoices). It has no view of how many jobs are in
each lifecycle state. The backend `JobStatus` enum has four states (Pending, Assigned,
EnRoute, Received), but there is no endpoint that returns counts per state.

Goal: add a **Job Status Cards** row between the metric cubes and the Recent Activity
("Recent Jobs") section. The row shows one card per backend state, and each count comes
from the database through the whole stack:

`Jobs.Status` (SQL) → `JobRepository.GetJobStatusCountsAsync` → `JobService.GetJobStatusSummaryAsync`
→ `GET /api/job/status-summary` → `apiService.getJobStatusSummary` → `useJobStatusSummary`
→ `JobStatusCards`.

| Card | Status key | Colour | Icon (lucide-react) |
|---|---|---|---|
| Pending | `Pending` | `#3B82F6` | `Hourglass` |
| Assigned | `Assigned` | `#0EA5E9` | `ClipboardList` |
| En Route | `EnRoute` | `#0284C7` | `Truck` |
| Received | `Received` | `#0369A1` | `Inbox` |

Production components contain no mock data. The data hook is shaped so that a later
SignalR change can push updates into the same query cache without touching the
presentational component.

## Scale

- `documentation-impact: extends-component`
- `impact-components`: `admin-dashboard`, `Api`. Both are existing catalog components.
  No new component, no new infrastructure, no new package.

## Decisions

| ID | Question | Answer / decision | Mitigating and supporting information | Status / approval provenance |
|---|---|---|---|---|
| D1 | Transport: REST or SignalR? | REST `GET /api/job/status-summary` on `JobController`, policy `ManagerOrAdmin`. | The request allows either. REST fits the existing TanStack Query pattern in `DashboardOverview` and the existing `JobController` routes (`by-status/{status}` uses the same policy). SignalR is not added now: the client's event name is already mismatched with the server's (`JobStatusUpdate` vs `JobStatusUpdated`, tracked in [todo](../todo/admin-dashboard-job-status-unresolved.md)). Live updates come later (see D6). Source: existing controller pattern. | ANSWERED — autonomous: recommendation accepted (source: existing `JobController` routes and dashboard query pattern) |
| D2 | Response contract shape | A JSON array, always holding exactly four items in enum order: `[{"status":"Pending","count":n},{"status":"Assigned",…},{"status":"EnRoute",…},{"status":"Received",…}]`. The C# side is `sealed record JobStatusCountDto(string Status, int Count)`. A state with no rows returns `count: 0`. | `Program.cs` uses `AddNewtonsoftJson` without a `StringEnumConverter`, so a raw enum would serialize as an int. Sending the enum name as a string keeps the wire format readable and independent of the enum's numeric values. Newtonsoft's ASP.NET Core default resolver writes camelCase names. Changing a public contract later is expensive. The array form lets a future status be added without breaking the shape. | ANSWERED — autonomous: low-confidence, no authoritative source (cost if wrong: one DTO, one TS type and one spec change) |
| D3 | Where does the DTO live? | `3-Domain/HotshotLogistics.Contracts/Models/JobStatusCountDto.cs`, namespace `HotshotLogistics.Contracts.Models`, `sealed record`. | The [C# standard](../standards/csharp/README.md) names `HotshotLogistics.Contracts.Models` as the target location for shared DTOs and requires `sealed record` response DTOs. Existing DTOs sit in `3-Domain/HotshotLogistics.Domain/DTOs` as classes. That divergence is already tracked in [contracts-model-placement](../todo/contracts-model-placement.md) and [api-platform-hardening](../todo/api-platform-hardening.md). The documented rule wins, so this DTO is not copied into Domain. No separate `Contracts.Models` project exists, so the type goes in a `Models/` folder of the Contracts project under the target namespace. That keeps project references unchanged. | ANSWERED — autonomous: low-confidence, no authoritative source (the standard names the namespace, not the project. Cost if wrong: moving one file) |
| D4 | What does the repository return? | `Task<IReadOnlyDictionary<JobStatus, int>> GetJobStatusCountsAsync(CancellationToken cancellationToken = default)`. It runs one parameterless `SELECT Status, COUNT(*) FROM Jobs GROUP BY Status` and fills in zero for every enum value not returned. | One aggregate round trip, with no client-side counting over pages. Mapping to the HTTP DTO stays in the service layer, so the persistence code does not know the wire shape. Follows the [data access layer standard](../standards/data-access-layer/README.md) (ADO.NET, `await using`, `CancellationToken`). | ANSWERED — autonomous: recommendation accepted (source: DAL standard, existing `GetJobCountAsync`) |
| D5 | The request names the card interface `JobStatus`, but `JobStatus` is already the enum in `src/types/index.ts` and `DashboardOverview` imports it. What name? | Export `interface JobStatusCard { label: string; count: number; color: string; icon: string }` from `src/components/dashboard/JobStatusCards.tsx`. The field set is the one requested. | Using the requested name would clash with the existing enum import. Renaming the stale enum belongs to the todo-tracked follow-up, which is out of scope. The fields are the ones requested, and only the type name changes. | ANSWERED — autonomous: low-confidence, no authoritative source (cheap: a local rename) |
| D6 | How are the components split, and how is the design kept ready for live updates? | (a) `useJobStatusSummary` hook: TanStack Query, key `['job-status-summary']`. It calls `apiService.getJobStatusSummary()` and maps the response to `JobStatusCard[]` using a fixed status→label/colour/icon table. (b) Presentational `JobStatusCards({ statuses }: { statuses: JobStatusCard[] })`, which has no fetching. (c) `DashboardOverview` calls the hook and renders the component, or its loading/error state. A later SignalR handler can call `queryClient.setQueryData(['job-status-summary'], …)` or `invalidateQueries` with no component change. | The [React standard](../standards/react/README.md) asks for one responsibility per component, one component per file, and repeated logic as a hook. The hook sits in `src/components/dashboard/` because the repo has no `src/hooks/` folder, and the dashboard Playwright gate covers files in that folder by convention (`task-e2e.sh` area `dashboard`). | ANSWERED — autonomous: recommendation accepted (source: React standard, existing query usage) |
| D7 | The requested grid is `grid grid-cols-5 gap-4`, which is not responsive and leaves an empty fifth column for four cards. What grid? | `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4`. | This uses the same breakpoints as the existing `stats-container` (`grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`) and keeps the requested `gap-4`. It meets "responsive grid" and lines the row up with the metric cubes. Cheap to reverse (one class string). | ANSWERED — autonomous: low-confidence, no authoritative source |
| D8 | Card styling | Card: `bg-white p-6 rounded-lg shadow` (same as `stats-card`). Label row: icon badge `w-8 h-8 rounded-md` with inline `style={{ backgroundColor: color }}` and a white `h-5 w-5` lucide icon, left of a `text-sm font-semibold text-gray-700` label. Count: `text-2xl font-bold text-gray-900 text-center` below the label. Font: inherits Inter from `src/app/layout.tsx` (`next/font/google`), so no font change is needed. Display labels: `Pending`, `Assigned`, `En Route`, `Received`. | Hex colours are data on `JobStatusCard.color`, so an inline background avoids dynamic Tailwind class names that the compiler cannot see. The label for `EnRoute` gets a space for readability, and the status key stays `EnRoute`. | ANSWERED — autonomous: low-confidence, no authoritative source (cheap, local to T3) |
| D9 | Loading, empty and error states, given that mock data is not allowed | Loading: the cards container shows `aria-busy="true"` with no numbers. Error: a `data-testid="job-status-cards-error"` message ("Unable to load job status counts"), with no counts and no fallback numbers. Zero rows: four cards showing `0`, which comes from the server (D2/D4). | Showing fallback or zero numbers on error would display invented data. The existing `__BYPASS_AUTH__` fallback block in `DashboardOverview` is pre-existing debt, and the new code must not extend it. | ANSWERED — autonomous: recommendation accepted (source: request "no mock data in production components") |
| D10 | Testing scope and isolation of test data | Only the tests needed to prove database values reach the display: (1) a repository DB test, (2) service and controller unit tests, (3) one HTTP integration test against the test DB, (4) Playwright for card rendering, placement, styling and responsive layout, against a mocked `/api/job/status-summary`. DB tests run against `hotshot_logistics_test` (started by `7-Deployment/dev-env/dev-env-up.sh` through `5-Test/run-task-tests.sh`). They insert their own uniquely-id'd rows, clean them up with the existing `TestRowCleanup`, and assert **deltas** (count after minus count before) so seed or other data cannot affect the result. | `GROUP BY` counts the whole table, so asserting deltas inside the serialized `DatabaseCollection` keeps the tests away from production and seed data without adding a test-only filter to production code. Playwright never calls the real API, following the existing spec's `page.route` pattern. | ANSWERED — autonomous: recommendation accepted (source: [test standard](../standards/test/README.md), existing `JobRepositoryTests` / `JobControllerIntegrationTests` fixtures) |
| D11 | Frontend version check | The dashboard is **Next.js 16.4.6 App Router** (`src/app/`), React 19.3.1, Tailwind CSS 4.3.5 via `@tailwindcss/postcss`, TanStack Query 5, lucide-react ^1.70.0 (exports `Hourglass`, `ClipboardList`, `Truck`, `Inbox`). No upgrade and no new dependency. | Verified from `1-Presentation/admin-dashboard/package.json` and `src/app/layout.tsx` at `ff86948`. | ANSWERED — autonomous: recommendation accepted (source: repository files) |
| D12 | The [data access layer standard](../standards/data-access-layer/README.md) says "Never `new SqlConnection(...)` at a call site", but no `ISqlConnectionFactory` exists, and every `JobRepository` and `BaseRepository` method opens its own `new SqlConnection(ConnectionString)`. How does T1 obtain its connection? | T1 keeps the existing repository convention: `await using SqlConnection connection = new(ConnectionString);`, the same as the sibling `GetJobCountAsync`. It adds no factory, no stub, and no new `BaseRepository` member. The new method is added to the migration scope of the [connection-factory todo](../todo/sql-connection-factory.md), and the T1 worker reports it as an `Open item:`. | The standard calls the factory "the target" and delegates its introduction to the connection-factory todo, which plans the factory, its DI registration and the migration of every call site in one change. Adding the factory here would be new cross-cutting infrastructure (AGENTS.md: needs approval) and would widen T1 beyond its scope. A one-off `BaseRepository` seam would change a shared `0-Base` abstraction ahead of the factory design and might conflict with it. The existing `BaseRepository` helpers do not fit: `ExecuteScalarAsync` returns a single value, `ExecuteQueryAsync` maps rows to `Job`, and neither accepts a `CancellationToken`, which the standard and D4 require. Cost if wrong: one more call site for the factory migration to sweep, and that migration already has to sweep every repository method. Raised by T1's `STATUS: BLOCKED` on 2026-11-16. | ANSWERED — autonomous: low-confidence, no authoritative source (the standard names the target but gives no interim rule. Cost if wrong: one call site) |
| AP1 | Approve and execute | Approved for execution as planned. | Recorded by the finalize step, which changed no task, file scope, test row or command. | ANSWERED: autonomous: recommendation accepted, AUTONOMOUS_APPROVAL: approve-and-execute, 2026-11-16 |

## Design harvest

| Target component | Target doc type | Section | Content to harvest |
|---|---|---|---|
| admin-dashboard | architecture (`6-Docs/admin-dashboard/architecture.md`) | Components and interfaces | `JobStatusCards` (presentational) and `useJobStatusSummary` (query key `['job-status-summary']`, the extension point for future SignalR pushes) under `src/components/dashboard/`, rendered by `DashboardOverview` between the metric cubes and Recent Activity. |
| admin-dashboard | architecture (`6-Docs/admin-dashboard/architecture.md`) | Data models | `JobStatusCount` (`{ status: string; count: number }`) in `src/types/index.ts`, which mirrors `JobStatusCountDto`. `JobStatusCard` view model. |
| admin-dashboard | architecture (`6-Docs/admin-dashboard/architecture.md`) | Error handling | The status cards show an explicit error state and never fallback numbers (D9). |
| Api | architecture (`6-Docs/api/architecture.md`) | Components and interfaces | `GET /api/job/status-summary` (`ManagerOrAdmin`) → `IJobService.GetJobStatusSummaryAsync` → `IJobRepository.GetJobStatusCountsAsync`. It returns four items in enum order, with zero for absent states. |
| Api | architecture (`6-Docs/api/architecture.md`) | Data models | `JobStatusCountDto` is the first shared DTO in the target `HotshotLogistics.Contracts.Models` namespace (D3). The section's sentence "Request and response DTOs live in Domain" is corrected to describe the target location and the tracked migration. |

## Investigation findings

Revision read: `ff86948` on `copilot/feat/job-status-cards` (in place, no worktree).
CodeGraph and kw-docs provenance matched `rev=ff86948 branch=copilot/feat/job-status-cards`.

Discovery: codegraph 2, docs 4, direct reads 24. Reasons for the direct reads:

- CodeGraph's `DashboardOverview.tsx` result was trimmed and omitted the JSX block positions, so the file was read directly.
- `Program.cs` JSON setup was not in the CodeGraph answer (read to settle D2).
- Several files are not indexed by CodeGraph, so they were read directly: `package.json`, `playwright.config.ts`, `src/app/layout.tsx`, `scripts/task-e2e.sh`, `5-Test/run-task-tests.sh`, `.kyber-weave/kyber-weave.yml`, and the test classes and helpers (`JobRepositoryTests`, `JobControllerTests`, `ServiceTests.cs`, `JobControllerIntegrationTests`, `TestRowCleanup`).
- The todo, standards and archived plan Markdown were read for exact rule text and section shape, after the coding-standard lookups.

Facts:

1. **Frontend stack** (D11): Next.js 16.4.6 App Router, React 19.3.1, Tailwind 4.3.5, TanStack Query 5, `@microsoft/signalr` 10, lucide-react. Inter is applied in `src/app/layout.tsx` through `next/font/google`. The repo has no frontend unit-test runner (tracked in [frontend-unit-test-tooling](../todo/frontend-unit-test-tooling.md)), so frontend behaviour is verified with Playwright.
2. **Dashboard home**: `src/app/page.tsx` → `DashboardLayout` → `src/components/dashboard/DashboardOverview.tsx`.
   - The metric grid is `div[data-testid="stats-container"]` with classes `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6`.
   - Each card is `[data-testid="stats-card"]` with classes `bg-white p-6 rounded-lg shadow`. The badge is `w-8 h-8 rounded-md`, the label is `text-sm font-medium text-gray-500`, and the value is `text-2xl font-semibold`.
   - The "Recent Jobs" block follows the grid directly.
   - The component already uses `useQuery` for `['jobs']`, `['drivers']` and `['invoices']`.
3. **API client**: `src/services/api.ts`.
   - Class `ApiService` with a private `request<T>(endpoint)` against `NEXT_PUBLIC_API_BASE_URL`. Playwright uses `https://localhost:5001/api`. The singleton is exported as `apiService`.
   - The existing summary-style call is `getInvoiceSummary()`.
4. **Backend enum**: `0-Base/HotshotLogistics.Core/Enums/JobStatus.cs` defines `Pending=0, Assigned=1, EnRoute=2, Received=3`. It is stored as an int in `Jobs.Status`.
5. **Repository**:
   - `IJobRepository` is in `3-Domain/HotshotLogistics.Contracts/Repositories/IJobRepository.cs`. Its only implementer is `4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs`.
   - `JobRepository` extends `BaseRepository`, uses `ConnectionString` and `GetTableName()` (`"Jobs"`), and its existing `GetJobCountAsync` shows the ADO.NET pattern to copy.
6. **Service**:
   - `IJobService` is in `3-Domain/HotshotLogistics.Contracts/Services/IJobService.cs`. Its only implementer is `2-Application/HotshotLogistics.Application/Services/JobService.cs`.
   - The `JobService` constructor takes `(IJobRepository, ICustomerRepository, IDriverRepository, INotificationService, IMappingService, ILogger<JobService>)`.
7. **Controller**: `1-Presentation/HotshotLogistics.Api/Controllers/JobController.cs`.
   - Class-level `[Authorize]`, `[Route("api/[controller]")]`.
   - The constructor takes `(IJobService, IJobRepository, ILogger<JobController>, IValidator<Job>)`.
   - The existing `GetJobsByStatus` is `[HttpGet("by-status/{status}")]` with `ManagerOrAdmin`.
   - Some actions call the repository directly. The new action goes through `IJobService` (Clean Architecture standard). The direct-repository calls are not changed here.
8. **JSON**: `Program.cs` uses `AddControllers().AddNewtonsoftJson(...)` with no enum converter (D2).
9. **Tests and gates**:
   - Backend: `5-Test/run-task-tests.sh '<filter>' <artifacts>`. It starts SQL Server for DB-backed projects against `hotshot_logistics_test` and exits 8 when no test ran.
   - `review.test-map` maps `JobController.cs` → `HotshotLogistics.IntegrationTests.JobControllerIntegrationTests`.
   - Frontend gates: lint, typecheck, build and `scripts/task-e2e.sh`. The script maps `src/components/dashboard/*` to `tests/dashboard*.spec.ts`.
10. **Divergences from documented rules** (recorded, not fixed here):
    - DTOs sit in `HotshotLogistics.Domain/DTOs` as classes, against the C# standard's `Contracts.Models` / `sealed record`. Tracked in [contracts-model-placement](../todo/contracts-model-placement.md) and [api-platform-hardening](../todo/api-platform-hardening.md).
    - `6-Docs/api/architecture.md` says "Request and response DTOs live in Domain". docs-dev corrects this in closeout.
    - No `ISqlConnectionFactory` exists. `BaseRepository` and every repository create `new SqlConnection(ConnectionString)` directly, against the DAL standard's connection rule. Tracked in [sql-connection-factory](../todo/sql-connection-factory.md). T1 follows the existing pattern under D12. (Found at `722b80c` after T1 returned BLOCKED. CodeGraph and kw-docs provenance matched `rev=722b80c branch=copilot/feat/job-status-cards`.)
    - `DashboardOverview` keeps test-mode fallback data and a stale `JobStatus` enum and SignalR event name. Tracked in [admin-dashboard-job-status-unresolved](../todo/admin-dashboard-job-status-unresolved.md). That todo refers to an earlier plan of the same name that is not present in the plans index or archive. This plan delivers the cards. It does not resolve that todo's three deferred findings.

## Test contract (development-mode: test-first)

| Task | Test project or file | Runner command | Observable behavior | RED evidence required | GREEN acceptance |
|---|---|---|---|---|---|
| T1 | `5-Test/tests/HotshotLogistics.Tests/Jobs/JobRepositoryTests.cs`: extend `HotshotLogistics.Tests.Jobs.JobRepositoryTests` (uses `[DatabaseFact]`, `TestRowCleanup`, unique `_customerId`) | `bash 5-Test/run-task-tests.sh 'FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobRepositoryTests' .agent_scratchpad/T1/artifacts` | `GetJobStatusCountsAsync_ReturnsCountPerStatusFromDatabase`: inserting 2 Pending, 1 Assigned, 3 EnRoute and 1 Received of the test's own jobs raises each state's count by exactly that delta. `GetJobStatusCountsAsync_ReturnsAllFourStatuses`: the result has exactly four keys (`Pending`, `Assigned`, `EnRoute`, `Received`), each ≥ 0, including states with no rows. | Compile failure CS1061: `IJobRepository`/`JobRepository` has no `GetJobStatusCountsAsync`, in the new test methods | Both new tests and all existing `JobRepositoryTests` pass against the test DB, with no assertion weakened |
| T2 | `5-Test/tests/HotshotLogistics.Tests/Utils/Infrastructure/ServiceTests.cs`: extend `HotshotLogistics.Tests.Utils.Infrastructure.JobServiceTests`. `5-Test/tests/HotshotLogistics.Tests/Jobs/JobControllerTests.cs`: extend `HotshotLogistics.Tests.Jobs.JobControllerTests`. `5-Test/tests/HotshotLogistics.IntegrationTests/JobControllerIntegrationTests.cs`: extend `HotshotLogistics.IntegrationTests.JobControllerIntegrationTests` (uses `CreateOwnJobAsync`, `Cleanup.TrackJob`, `Authorization: Test`) | `bash 5-Test/run-task-tests.sh 'FullyQualifiedName~HotshotLogistics.Tests.Utils.Infrastructure.JobServiceTests\|FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobControllerTests\|FullyQualifiedName~HotshotLogistics.IntegrationTests.JobControllerIntegrationTests\|FullyQualifiedName~HotshotLogistics.Tests.Utils.Infrastructure.ArchitectureTests' .agent_scratchpad/T2/artifacts` | Service: `GetJobStatusSummaryAsync_MapsRepositoryCountsToDtosInEnumOrder` turns repository counts `{Pending:4, Assigned:0, EnRoute:2, Received:1}` into four `JobStatusCountDto` items in the order Pending, Assigned, EnRoute, Received, with those counts, and passes the cancellation token through. Controller: `GetJobStatusSummary_ReturnsOkWithServiceSummary` returns `OkObjectResult` with the service's list, and does not call `IJobRepository`. Integration: `GetJobStatusSummary_ReflectsDatabaseCounts` sends `GET /api/job/status-summary` with `Authorization: Test`. It returns 200 and a JSON array of four `{status,count}` objects with string names `Pending`, `Assigned`, `EnRoute`, `Received` in that order. After the test creates its own Pending job with `CreateOwnJobAsync`, the `Pending` count is exactly one higher than before. `ArchitectureTests` stay green. | Compile failure CS1061/CS0246: `IJobService.GetJobStatusSummaryAsync`, `JobStatusCountDto` and `JobController.GetJobStatusSummary` are missing, in the new tests | All named classes pass, including the existing tests, with no assertion weakened |
| T3 | `1-Presentation/admin-dashboard/tests/dashboard-overview.spec.ts`: extend the `Dashboard Overview` describe; copy its `page.route('https://localhost:5001/api/job', …)` mock pattern | `bash 1-Presentation/admin-dashboard/scripts/task-e2e.sh 1-Presentation/admin-dashboard/tests/dashboard-overview.spec.ts` (equivalently `cd 1-Presentation/admin-dashboard && npx playwright test tests/dashboard-overview.spec.ts --project=chromium`) | New `test.describe('Job Status Cards')`, with `https://localhost:5001/api/job/status-summary` mocked to `[{status:'Pending',count:7},{status:'Assigned',count:5},{status:'EnRoute',count:3},{status:'Received',count:2}]`, and that mock also added to the shared `beforeEach` so no test reaches a real API. Tests: (1) `should render four status cards with API counts in order`: `[data-testid="job-status-cards"]` holds four cards with labels Pending, Assigned, En Route, Received and counts 7, 5, 3, 2. (2) `should place status cards between metrics and recent activity`: the container's top is below the bottom of `stats-container` and above the "Recent Jobs" heading. (3) `should match metric card styling and colours`: each card's computed `border-radius`, `box-shadow` and `padding` equal those of a `[data-testid="stats-card"]`, each badge's computed background equals rgb of #3B82F6, #0EA5E9, #0284C7 and #0369A1 respectively, and the label `font-family` contains `Inter`. (4) `should lay out responsively`: at 375×800 the cards stack (equal `x`), and at 1280×800 all four share one row (equal `y`). (5) `should show an error state without counts when the API fails`: a 500 response shows `job-status-cards-error` and no `job-status-count-*` element. | The new tests fail with locator timeouts on `[data-testid="job-status-cards"]` before implementation (spec added on its own first) | All five new tests and the existing `dashboard-overview.spec.ts` tests pass in chromium. Lint, typecheck and build gates pass |
| T4 | No test surface: documentation only | `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .` | Harvest rows land in the two architecture docs | n/a | Both checks report no new findings for the touched docs |

## Tasks

### T1: Repository aggregate of job counts per status

- **Objective:** Add a database aggregate that returns the number of jobs in each `JobStatus` state, with zero for every state that has no rows.
- **Capabilities:** C# ADO.NET data access with xUnit database tests.
- **Session:** `backend-data`
- **Depends on:** none
- **Files:**
  - `3-Domain/HotshotLogistics.Contracts/Repositories/IJobRepository.cs`: interface `IJobRepository`, add `Task<IReadOnlyDictionary<JobStatus, int>> GetJobStatusCountsAsync(CancellationToken cancellationToken = default);` with an XML doc comment.
  - `4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs`: class `JobRepository`, implement `GetJobStatusCountsAsync`. Copy the shape of the existing `GetJobCountAsync` (`await using SqlConnection connection = new(ConnectionString)`, `OpenAsync(cancellationToken)`, `await using SqlCommand`). Decision D12 sanctions this direct connection as the interim pattern until the connection-factory todo is delivered. Do not add an `ISqlConnectionFactory`, a stub, or a `BaseRepository` member. Report `Open item: 4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs — GetJobStatusCountsAsync opens a SqlConnection directly (D12) — in scope of todo sql-connection-factory`. Query `$"SELECT Status, COUNT(*) FROM {GetTableName()} GROUP BY Status"`, read the rows with `ExecuteReaderAsync(cancellationToken)`, and return a dictionary seeded with every `Enum.GetValues<JobStatus>()` value at 0. Status values not defined in the enum are ignored.
  - `5-Test/tests/HotshotLogistics.Tests/Jobs/JobRepositoryTests.cs`: class `HotshotLogistics.Tests.Jobs.JobRepositoryTests`, add `GetJobStatusCountsAsync_ReturnsCountPerStatusFromDatabase` and `GetJobStatusCountsAsync_ReturnsAllFourStatuses` as `[DatabaseFact]` tests. Insert jobs with the class's unique `_customerId` and ids, track them for cleanup the way the existing tests do, read the counts before and after inserting, and assert exact deltas.
- **Commands:**
  - Failing run first: `kyber-weave review gates . --agent <you> --task T1 --red`
  - Then: `bash 5-Test/run-task-tests.sh 'FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobRepositoryTests' .agent_scratchpad/T1/artifacts`
  - Then: `kyber-weave review gates . --agent <you> --task T1`
- **Acceptance:**
  - The Test contract T1 row passes.
  - The query uses no string-concatenated user input.
  - Every I/O call receives the `CancellationToken`.
  - No other repository member changes.

### T2: Service, DTO and REST endpoint for the status summary

- **Objective:** Expose the per-status counts as `GET /api/job/status-summary`, following controller → service → repository, and return four `JobStatusCountDto` items in enum order.
- **Capabilities:** C# ASP.NET Core service and controller with xUnit unit and HTTP integration tests.
- **Session:** `backend-api`
- **Depends on:** T1 (consumes `IJobRepository.GetJobStatusCountsAsync`).
- **Files:**
  - New `3-Domain/HotshotLogistics.Contracts/Models/JobStatusCountDto.cs`: `namespace HotshotLogistics.Contracts.Models;` `public sealed record JobStatusCountDto(string Status, int Count);` with XML doc. Do not place it in the Domain project's DTOs folder (D3).
  - `3-Domain/HotshotLogistics.Contracts/Services/IJobService.cs`: interface `IJobService`, add `Task<IReadOnlyList<JobStatusCountDto>> GetJobStatusSummaryAsync(CancellationToken cancellationToken = default);`.
  - `2-Application/HotshotLogistics.Application/Services/JobService.cs`: class `JobService`, implement `GetJobStatusSummaryAsync`. Call `_jobRepository.GetJobStatusCountsAsync(cancellationToken)` and project `Enum.GetValues<JobStatus>()` in order to `new JobStatusCountDto(status.ToString(), counts.GetValueOrDefault(status))`.
  - `1-Presentation/HotshotLogistics.Api/Controllers/JobController.cs`: class `JobController`, add `[HttpGet("status-summary")] [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)] [ProducesResponseType(typeof(IReadOnlyList<JobStatusCountDto>), StatusCodes.Status200OK)] public async Task<ActionResult<IReadOnlyList<JobStatusCountDto>>> GetJobStatusSummary(CancellationToken cancellationToken = default)`. It returns `Ok(await _jobService.GetJobStatusSummaryAsync(cancellationToken))`. Declare it so the literal route `status-summary` is not captured by `[HttpGet("{id}")]`; attribute routing prefers literal segments.
  - `5-Test/tests/HotshotLogistics.Tests/Utils/Infrastructure/ServiceTests.cs`: class `HotshotLogistics.Tests.Utils.Infrastructure.JobServiceTests`, add `GetJobStatusSummaryAsync_MapsRepositoryCountsToDtosInEnumOrder`.
  - `5-Test/tests/HotshotLogistics.Tests/Jobs/JobControllerTests.cs`: class `HotshotLogistics.Tests.Jobs.JobControllerTests`, add `GetJobStatusSummary_ReturnsOkWithServiceSummary`.
  - `5-Test/tests/HotshotLogistics.IntegrationTests/JobControllerIntegrationTests.cs`: class `HotshotLogistics.IntegrationTests.JobControllerIntegrationTests`, add `GetJobStatusSummary_ReflectsDatabaseCounts` using the existing `CreateOwnJobAsync` helper.
- **Commands:**
  - Failing run first: `kyber-weave review gates . --agent <you> --task T2 --red`
  - Then: `bash 5-Test/run-task-tests.sh 'FullyQualifiedName~HotshotLogistics.Tests.Utils.Infrastructure.JobServiceTests|FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobControllerTests|FullyQualifiedName~HotshotLogistics.IntegrationTests.JobControllerIntegrationTests|FullyQualifiedName~HotshotLogistics.Tests.Utils.Infrastructure.ArchitectureTests' .agent_scratchpad/T2/artifacts`
  - Then: `kyber-weave review gates . --agent <you> --task T2`
- **Acceptance:**
  - The Test contract T2 row passes.
  - The action never returns an entity and never calls `IJobRepository` directly.
  - The JSON is a camelCase array of `{status,count}` with string status names.

### T3: Job Status Cards component on the Admin Dashboard

- **Objective:** Render the four status cards from live API data between the metric cubes and Recent Activity, with the agreed styling, responsive layout and error state.
- **Capabilities:** React and TypeScript UI with TailwindCSS, TanStack Query and Playwright tests.
- **Session:** `frontend`
- **Depends on:** none. The wire contract is fixed by D2, and the spec mocks it.
- **Files:**
  - `1-Presentation/admin-dashboard/src/types/index.ts`: add `export interface JobStatusCount { status: string; count: number }`. Leave the existing `JobStatus` enum unchanged.
  - `1-Presentation/admin-dashboard/src/services/api.ts`: class `ApiService`, add `async getJobStatusSummary(): Promise<JobStatusCount[]> { return this.request<JobStatusCount[]>('/job/status-summary'); }`.
  - New `1-Presentation/admin-dashboard/src/components/dashboard/useJobStatusSummary.ts`: hook `useJobStatusSummary()`. It calls `useQuery({ queryKey: ['job-status-summary'], queryFn: () => apiService.getJobStatusSummary() })` and maps the data through a fixed `Pending/Assigned/EnRoute/Received → {label, color, icon}` table (labels `Pending`, `Assigned`, `En Route`, `Received`; colours `#3B82F6`, `#0EA5E9`, `#0284C7`, `#0369A1`; icons `Hourglass`, `ClipboardList`, `Truck`, `Inbox`) into `JobStatusCard[]` in that fixed order. It returns `{ statuses, isLoading, isError }`. A status missing from the response is not invented: the API guarantees all four (D2).
  - New `1-Presentation/admin-dashboard/src/components/dashboard/JobStatusCards.tsx`:
    - `export interface JobStatusCard { label: string; count: number; color: string; icon: string }`.
    - Default component `JobStatusCards({ statuses }: { statuses: JobStatusCard[] })`. It maps `icon` through a `{ Hourglass, ClipboardList, Truck, Inbox }` lookup from `lucide-react`.
    - Container: `div[data-testid="job-status-cards"]` with classes `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4`.
    - Each card: `data-testid="job-status-card-<key>"`, classes `bg-white p-6 rounded-lg shadow`. Its label row has the badge `data-testid="job-status-badge-<key>"` (`w-8 h-8 rounded-md flex items-center justify-center`, `style={{ backgroundColor: color }}`, white `h-5 w-5` icon, `aria-hidden`) next to the label (`text-sm font-semibold text-gray-700`). Below it sits the count, `data-testid="job-status-count-<key>"` (`text-2xl font-bold text-gray-900 text-center`). `<key>` is the lowercase status key: `pending`, `assigned`, `enroute`, `received`.
    - No data fetching and no mock data.
  - `1-Presentation/admin-dashboard/src/components/dashboard/DashboardOverview.tsx`: component `DashboardOverview`. Call `useJobStatusSummary()` and render, between the `stats-container` grid and the Recent Activity ("Recent Jobs") block, either:
    - `<JobStatusCards statuses={statuses} />`, or
    - the loading state (`data-testid="job-status-cards"` with `aria-busy="true"` and no numbers), or
    - the error state (`data-testid="job-status-cards-error"`, text "Unable to load job status counts").

    Add nothing to the existing `__BYPASS_AUTH__` fallback block.
  - `1-Presentation/admin-dashboard/tests/dashboard-overview.spec.ts`: add the `status-summary` route mock to the shared `beforeEach`, and add `test.describe('Job Status Cards')` with the five tests in the Test contract T3 row.
- **Commands:**
  - Failing run first: `kyber-weave review gates . --agent <you> --task T3 --red`
  - Then: `bash 1-Presentation/admin-dashboard/scripts/task-e2e.sh 1-Presentation/admin-dashboard/tests/dashboard-overview.spec.ts`
  - Then: `kyber-weave review gates . --agent <you> --task T3` (runs lint, typecheck, build and e2e)
- **Acceptance:**
  - The Test contract T3 row passes.
  - No new npm dependency.
  - No hard-coded counts in `src/`.

### T4: Documentation harvest

- **Objective:** Record the new interfaces and data models in the two component architecture documents, following the Design harvest table.
- **Capabilities:** documentation.
- **Session:** `docs`
- **Depends on:** T2, T3 (documents their delivered contracts).
- **Files:**
  - `6-Docs/admin-dashboard/architecture.md`: sections "Components and interfaces", "Data models" and "Error handling".
  - `6-Docs/api/architecture.md`: sections "Components and interfaces" and "Data models". In "Data models", correct the sentence "Request and response DTOs live in Domain" to name the D3 target namespace for shared DTOs and link the contracts-model-placement todo.
  - `6-Docs/catalog.md`: bump the `last-reviewed` dates on the `admin-dashboard` and `Api` rows.
- **Commands:** `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .`
- **Acceptance:**
  - Every Design harvest row has landed.
  - Both docs checks report no new findings.

## Dependency graph

```text
T1 (backend-data) ──► T2 (backend-api) ──┐
                                          ├──► T4 (docs)
T3 (frontend) ────────────────────────────┘
```

- T1 and T3 start together (2 of the 4 concurrent workers).
- T2 starts after T1 because it consumes the new repository method.
- T4 runs last.
- No file is shared between tasks.

## Risks

1. **Delta assertions under concurrent DB writers.** The aggregate counts the whole `Jobs` table, so another test writing jobs at the same moment could disturb an exact delta. Mitigation:
   - Both DB-backed test classes run in the serialized `DatabaseCollection`.
   - `run-task-tests.sh` runs projects one after another.
   - The tests insert and clean up only their own rows.
2. **Route capture by `{id}`.** `GET /api/job/status-summary` could be read as `GetJobById("status-summary")`. Mitigation: ASP.NET Core attribute routing ranks literal segments ahead of parameters, and the integration test asserts a 200 array response.
3. **Enum/int drift.** An out-of-range `Status` value in the DB is ignored rather than surfacing as a fifth card. That is acceptable for this view. The four known states are always returned.
4. **Inter font assertion.** `next/font` generates family names such as `__Inter_xxxx`. The test checks that the name *contains* `Inter`, not that it equals it.
5. **Tailwind arbitrary colour classes.** These are avoided by using inline `backgroundColor` (D8), so no safelist is needed.

## Out-of-scope boundaries

- SignalR push of status counts. Fixing the `JobStatusUpdate`/`JobStatusUpdated` event mismatch, the stale frontend `JobStatus` enum, and the first-page Active Jobs count: all tracked in [admin-dashboard-job-status-unresolved](../todo/admin-dashboard-job-status-unresolved.md).
- Removing the `__BYPASS_AUTH__` test fallback data from `DashboardOverview` and making Recent Jobs data-driven ([admin-dashboard-playwright-baseline](../todo/admin-dashboard-playwright-baseline.md)).
- Moving the existing Domain DTOs to `Contracts.Models` / `sealed record` ([contracts-model-placement](../todo/contracts-model-placement.md), [api-platform-hardening](../todo/api-platform-hardening.md)).
- Changing the existing `JobController` actions that call `IJobRepository` directly.
- Frontend unit-test tooling ([frontend-unit-test-tooling](../todo/frontend-unit-test-tooling.md)).
- Schema changes, migrations, infrastructure, new packages, secrets, and configuration changes.

## Verification gates

1. **Per-task gates:** each developer task ends with `kyber-weave review gates . --agent <you> --task <id>`.
   - Backend tasks run the tests gate (`run-task-tests.sh`).
   - T3 runs `dashboard-lint`, `dashboard-typecheck`, `dashboard-build` and `dashboard-e2e`.
   - A failing gate goes back to the same worker.
2. **End-to-end data integrity:** the chain is proven link by link.
   - T1 proves DB rows → repository counts.
   - T2 proves repository → service DTO → HTTP JSON over the test database.
   - T3 proves the same JSON shape → rendered cards.
   - The shared wire contract is D2.
3. **Test-data isolation:** DB tests run only against `hotshot_logistics_test`, use their own unique rows with cleanup, and assert deltas (D10). Playwright never calls a real API.
4. **Docs gates:** `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .`. While this plan is open, only `KW-DOC-LIFECYCLE-003` for this plan may remain, and it clears when the plan is archived.
5. **CI:** the pull request's workflows are green.

## Review

`code-reviewer` reviews the whole accumulated change at the end of the run under the
`code-review` skill, which supplies its own invocation. This plan does not carry the
review command.

## docs-dev closeout

docs-dev does the harvest in T4 according to the Design harvest table, then runs the
closeout:
- confirms that each harvest row landed;
- maps every decision (D1–D11) to its implementation reference;
- leaves the linked out-of-scope todos open as `RESIDUAL`;
- reports the documentation-impact review.

The plan is then archived with its index row moved to Archived.

## Completion summary

- T1 `d11572a`: the repository aggregate seeds defined statuses, ignores undefined
  database values, and propagates cancellation. The T1 task gate reports tests passed;
  its RED gate accepted the missing repository method.
- T2 `3bafe30`: the service, DTO and authorized endpoint connect the aggregate to
  HTTP. The T2 task gate reports tests passed; its RED gate accepted the missing
  Contracts Models namespace. The named repository, service, controller and HTTP
  integration test methods are present in the committed test files.
- T3 `7a08d84`: the dashboard consumes the summary and renders the cards. Its task
  gate reports lint, typecheck, build and dashboard e2e passed; the RED gate
  recorded five failing new card tests. The red log also reported a pre-existing
  billing fixture error and SignalR connection failure during navigation.
- T4: the design harvest is recorded in the component architecture docs and
  component catalog dates updated. The documentation-impact review covers the
  accumulated T1–T3 changes and recommends updates to both architecture documents
  and catalog; those updates are included here.
- NOT VERIFIED: live database-to-dashboard behavior and manual browser appearance —
  these require a running environment and browser; mocked browser tests and the
  recorded database and HTTP gates cover the separate links.
- NOT VERIFIED: pull request CI — no pull request CI evidence was supplied.
- RESIDUAL: `GetJobStatusCountsAsync` opens a `SqlConnection` directly (D12), in
  scope of [sql-connection-factory](../todo/sql-connection-factory.md). Existing
  job controller actions return entities contrary to the C# standard, already
  tracked in [api-platform-hardening](../todo/api-platform-hardening.md). Other
  out-of-scope todos remain open.

DOCS_REVIEW: update — T1–T3 introduce an API contract and dashboard behavior.

RECOMMENDATIONS: `6-Docs/api/architecture.md`, `6-Docs/admin-dashboard/architecture.md`,
`6-Docs/catalog.md` — updated in T4.

## Closeout mapping

All rows have `status: archived` for the closeout record; the live destination
documents remain current. No ADR — ADR waiver: these decisions refine existing
component architecture, apply established patterns, or are implementation details.

| ID | Destination | documentation-impact | impact-components | last-reviewed | status |
|---|---|---|---|---|---|
| D1 | [API architecture: Job status summary](../../api/architecture.md#job-status-summary); [dashboard architecture: Components and interfaces](../../admin-dashboard/architecture.md#components-and-interfaces) | extends-component | Api, admin-dashboard | 2026-11-16 | archived |
| D2, D4 | [API architecture: Job status summary](../../api/architecture.md#job-status-summary) | extends-component | Api | 2026-11-16 | archived |
| D3 | [API architecture: Data models](../../api/architecture.md#data-models) | extends-component | Api | 2026-11-16 | archived |
| D5 | [Dashboard architecture: Data models](../../admin-dashboard/architecture.md#data-models) | extends-component | admin-dashboard | 2026-11-16 | archived |
| D6, D7, D8 | [Dashboard architecture: Components and interfaces](../../admin-dashboard/architecture.md#components-and-interfaces) | extends-component | admin-dashboard | 2026-11-16 | archived |
| D9 | [Dashboard architecture: Error handling](../../admin-dashboard/architecture.md#error-handling) | extends-component | admin-dashboard | 2026-11-16 | archived |
| D10 | [API architecture: Testing strategy](../../api/architecture.md#testing-strategy); [dashboard architecture: Testing strategy](../../admin-dashboard/architecture.md#testing-strategy) | extends-component | Api, admin-dashboard | 2026-11-16 | archived |
| D11 | [Technology stack](../../technology-stack.md); [dashboard architecture](../../admin-dashboard/architecture.md#overview) | extends-component | admin-dashboard | 2026-11-16 | archived |
| D12 | [API architecture: Job status summary](../../api/architecture.md#job-status-summary); [connection-factory todo](../../todo/sql-connection-factory.md) | extends-component | Api | 2026-11-16 | archived |
| AP1 | Documentation waiver — execution approval is a historical process event with no surviving behavioral invariant. | none | admin-dashboard, Api | 2026-11-16 | archived |
| H1, admin-dashboard | [Dashboard architecture: Components and interfaces](../../admin-dashboard/architecture.md#components-and-interfaces) | extends-component | admin-dashboard | 2026-11-16 | archived |
| H2 | [Dashboard architecture: Data models](../../admin-dashboard/architecture.md#data-models) | extends-component | admin-dashboard | 2026-11-16 | archived |
| H3 | [Dashboard architecture: Error handling](../../admin-dashboard/architecture.md#error-handling) | extends-component | admin-dashboard | 2026-11-16 | archived |
| H4, Api | [API architecture: Job status summary](../../api/architecture.md#job-status-summary) | extends-component | Api | 2026-11-16 | archived |
| H5 | [API architecture: Data models](../../api/architecture.md#data-models) | extends-component | Api | 2026-11-16 | archived |
