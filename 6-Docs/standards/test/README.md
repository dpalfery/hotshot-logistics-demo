---
id: standards/test
title: "Test coding standard"
doc-type: coding-standard
status: draft
technology: test
owner: unassigned
last-reviewed: 2026-11-14
---

# Test coding standard

How tests work in this repository. Agents resolve this as `<test-coding-standard>`. C# in test
files also follows `<csharp-coding-standard>`; dashboard code follows `<react-coding-standard>`.
While `status: draft` this is a proposal: say so and ask a human before relying on it.

## Projects and the task command

| Project (`5-Test/` unless noted) | Kind | Needs SQL Server |
|---|---|---|
| `tests/HotshotLogistics.Tests` | unit, repository, controller, architecture | some classes |
| `tests/HotshotLogistics.IntegrationTests` | HTTP controller tests (`WebApplicationFactory`) | yes |
| `HotshotLogistics.Api.Tests` | auth pipeline tests | no |
| `7-Deployment/DbSetup/HotshotLogistics.DbSetup.Tests` | DbSetup CLI | no |
| `1-Presentation/admin-dashboard/tests` | Playwright specs (no unit runner) | no |

Stack: xUnit v3, Moq (not NSubstitute), FluentAssertions 8 (`Assert.*` also appears), NetArchTest.

Run one task's backend tests (starts SQL Server itself when a project needs it; exit 8 = no test ran):

```bash
bash 5-Test/run-task-tests.sh 'FullyQualifiedName~HotshotLogistics.Tests.Jobs.JobRepositoryTests|FullyQualifiedName~HotshotLogistics.IntegrationTests.JobControllerIntegrationTests' .agent_scratchpad/<task>/artifacts
```

The gates in `.kyber-weave/kyber-weave.yml` (`review.task-gates`) pass `{tests}` = classes found via
`review.test-map` (controller to its `*ControllerIntegrationTests`), else the `<Stem>Tests` convention.
Dashboard: `bash 1-Presentation/admin-dashboard/scripts/task-e2e.sh <repo-relative changed files>` (chromium).

## Fixtures and collections

- `[Collection("DatabaseCollection")]` is a bare name (no `CollectionDefinition` class): xUnit runs
  same-named classes in one assembly serially. All 7 controller classes in `IntegrationTests` and
  `Tests/Jobs/JobControllerIntegrationTests` join it.
- **Rule:** every class that writes to the shared database or asserts on its contents joins it.
- Repository classes use `IClassFixture<DatabaseTestFixture>`; HTTP classes inherit
  `IntegrationTestBase` (factory + fixture) and send `Authorization: Test` (`X-Test-Role` picks the role).
- `[DatabaseFact]` (`Tests` project only) is skipped, not failed, when no connection string exists.
  A skipped DB test is not a pass: run `7-Deployment/dev-env/dev-env-up.sh` first.

## Database for integration tests

- Engine: SQL Server 2022 in container `hotshot_sqlserver` (port 1433). Run
  `7-Deployment/dev-env/dev-env-up.sh` (idempotent, lock-serialised, ~7-20 s); it creates database
  `hotshot_logistics_test`, login `hotshot_app` and applies migrations.
- Connection: user secret `ConnectionStrings:DefaultConnection` (shared by all worktrees) or env
  `CONNECTIONSTRINGS__DEFAULTCONNECTION`. Never write it into the repository.
- Schema: `4-Persistence/HotshotLogistics.Data/Migrations` (FluentMigrator). `IntegrationTests`'
  fixture also runs `MigrateUp`; the `Tests` fixture only checks connectivity.

## Seeded data

Safe to read (do not modify or delete): customers `cust-001`..`cust-100` ("Seed Customer 001"), jobs
`job-<customer>-<nnn>` (e.g. `job-cust-001-001`), assignments `ja-<jobId>`, invoices `inv-<jobId>`
(`inv-job-cust-001-001`), ~200 drivers `seed.driverNNN@local.test` (identity ids: look up by email),
plus `cust-1`, `cust-2`, `job-1`, `job-2` (`SeedInitialData`). Everything else, create in the test.
`CUST001`/`JOB001` in repository tests are **not** seeded (verified in the dev database); they are
free-text ids, not references to rely on.

## Test data hygiene

- Unique ids per test: `test-{Guid.NewGuid():N}`, `test-job-...`, GUID in emails and licence numbers.
- Repository classes record created ids in `_created*Ids` and delete them in `Dispose()`; delete tests
  call the API. **Rule:** cleanup runs in `finally`/`Dispose`, never skipped on failure.
- **Rule:** assert on count deltas or `>= n`, never absolute counts (seed and leftovers vary).

## Copy these (one per layer)

| Layer | Class (file under `5-Test/tests/`) |
|---|---|
| Repository (real DB) | `HotshotLogistics.Tests.Jobs.JobRepositoryTests` (`HotshotLogistics.Tests/Jobs/`); add the collection |
| Service (Moq) | `RealtimeServiceTests` (`HotshotLogistics.Tests/Communication/`) |
| Controller, unit | `JobControllerTests` (`HotshotLogistics.Tests/Jobs/`) |
| Controller, HTTP | `JobControllerIntegrationTests` (`HotshotLogistics.IntegrationTests/`) |
| Architecture | `ArchitectureTests` (`HotshotLogistics.Tests/Utils/Infrastructure/`) |
| Playwright | `1-Presentation/admin-dashboard/tests/billing-management.spec.ts` |

Builder: `CustomerBuilder` (`Utils/TestHelpers`); other tests build entities inline. Fakes are Moq mocks
of interfaces; the database is never mocked.

## Naming

Class `<Subject>Tests` or `<Controller>IntegrationTests`, file = class, namespace
`HotshotLogistics.Tests.<Feature>`. Method `Method_Scenario_Expected`
(`GetJobsAsync_WithFiltering_ReturnsCorrectResults`); architecture rules use snake case. Specs:
`<kebab-area>.spec.ts`, tests titled `should ...`.

## Parallel runs must not share

- Build output: use a per-task `--artifacts-path` (the gates do).
- The database: one shared `hotshot_logistics_test`; unique ids and collections protect it.
- Port 3100: set `PLAYWRIGHT_PORT` for a second dashboard run (`reuseExistingServer` is false).
- The dev container: start it only through `dev-env-up.sh` (it takes the lock).

## Frontend (Playwright)

Config `1-Presentation/admin-dashboard/playwright.config.ts`: specs in `tests/*.spec.ts`, `next dev` on
port 3100 over http, locale en-US, UTC, three browsers (gates use chromium). Every API call is mocked with
`page.route('**/api/...')` or `TestHelpers.setupBasicMocks()` (`tests/utils/test-helpers.ts`); fixtures in
`tests/fixtures/`. Never hit a real API. Register invoice routes per test, not in `setupBasicMocks`.

```bash
cd 1-Presentation/admin-dashboard && npx playwright test tests/billing-management.spec.ts --project=chromium
```

## Known gaps

- `JobRepositoryTests`, `InvoiceRepositoryTests`, `LocationTrackingRepositoryTests` are not in the
  database collection but share tables with the controller classes (no exact-count assertion found today).
- `Tests` and `IntegrationTests` each have their own `TestDatabaseHelper`/`DatabaseTestFixture`/
  `IntegrationTestBase` with different behaviour; their collections do not serialise against each other.
- Cleanup swallows errors and never uses `finally`; the dev database accumulates leftovers.
