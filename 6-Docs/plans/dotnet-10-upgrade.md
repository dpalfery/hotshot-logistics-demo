---
id: plans/dotnet-10-upgrade
title: .NET 10 upgrade and dependency refresh
doc-type: plan
status: ready
component: Api
owner: unassigned
last-reviewed: 2026-11-09
development-mode: test-first
documentation-impact: extends-component
impact-components:
  - Api
  - DbSetup
  - admin-dashboard
---

# .NET 10 upgrade and dependency refresh

**Status:** Ready — approved for execution 2026-11-09 (D10).

## Problem and goal

Every project in this repository targets `net8.0`. .NET 8 and .NET 9 leave support on
2026-11-10 (self-gathered), and the user's request is verbatim: "current project uses
.net 8 we need to upgrade to .net 10 latest and upgrade all dependencies".

**Goal:** every project that targets a framework today builds on `net10.0` with a
pinned .NET 10 SDK, the application's own dependencies (every NuGet package across
the solution plus the admin-dashboard npm packages, per the decision ledger) are
refreshed to current stable versions, the CI .NET SDK pin moves to 10.x (D5) and the deploy migration step's `net8.0`
path literal moves with the cutover (D9), with everything else in
`.github/workflows` deliberately untouched (D2), the existing test
suites pass on the upgraded target, and the documented stack facts under `6-Docs/`
and the component READMEs stop asserting .NET 8.

The user chose .NET 10 explicitly; that choice is not reopened here.

## Scale

- `documentation-impact: extends-component` — the upgrade extends the three existing
  catalog components and creates no new component.
- `impact-components`: `Api`, `DbSetup`, `admin-dashboard` (catalog names).

## Investigation findings

All discovery was performed as direct reads of the worktree at revision
`413b897740fd7d9f3e6fbc232d039e88ff2b33c8` (branch `factory/chore/dotnet-10-upgrade`),
because the CodeGraph and kw-docs MCP servers are bound to the canonical checkout and
were not available in this session. External version and breaking-change facts are
self-gathered (8 web searches, 1 fetched Microsoft Learn page); sources are named
inline. `6-Docs/` reads used the AGENTS.md fallback to the documentation index
because the kyber-weave MCP was unavailable.

Discovery: codegraph 0, docs 0, direct reads 34, targeted greps ~10.

### Solution inventory — 12 projects, all `net8.0`

| Project | Path | Notable packages (current) |
|---|---|---|
| HotshotLogistics.Core | `0-Base/HotshotLogistics.Core/HotshotLogistics.Core.csproj` | Azure.Identity 1.21.0, M.E.Configuration 10.0.11, M.E.Configuration.AzureAppConfiguration 8.6.0, Microsoft.Data.SqlClient 7.0.2 |
| HotshotLogistics.Api | `1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj` | Mvc.NewtonsoftJson 8.0.30, AppConfiguration.AspNetCore 8.6.0, Microsoft.Graph 5.105.0, Microsoft.Identity.Web 4.14.2, Azure.SignalR 1.33.1, Swashbuckle.AspNetCore 10.2.3 |
| HotshotLogistics.Application | `2-Application/HotshotLogistics.Application/HotshotLogistics.Application.csproj` | FluentValidation 12.1.1, Azure.SignalR.Management 1.33.1, Polly 8.7.0, Graph 5.105.0, Identity.Web 4.14.2 |
| HotshotLogistics.Domain | `3-Domain/HotshotLogistics.Domain/HotshotLogistics.Domain.csproj` | none |
| HotshotLogistics.Contracts | `3-Domain/HotshotLogistics.Contracts/HotshotLogistics.Contracts.csproj` | none |
| HotshotLogistics.Data | `4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj` | FluentMigrator(.Runner) 8.0.1, SqlClient 7.0.2, Bogus 35.6.5, NotificationHubs 4.2.0, SendGrid 9.29.3, Twilio 7.14.9, M.E.Hosting 10.0.11 |
| MigrationRunner | `4-Persistence/MigrationRunner/MigrationRunner.csproj` | FluentMigrator.Runner 8.0.1, Bogus 35.6.5, M.E.Configuration.* 10.0.11 |
| HotshotLogistics.Api.Tests | `5-Test/HotshotLogistics.Api.Tests/HotshotLogistics.Api.Tests.csproj` | Test.Sdk 18.8.1, xunit.v3 3.2.2, Mvc.Testing 8.0.30, FluentAssertions 8.10.0, Moq 4.20.72 |
| HotshotLogistics.Tests | `5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj` | Test.Sdk 18.8.1, xunit.v3 3.2.2, Mvc.Testing 8.0.30, TestHost 8.0.30, NetArchTest.Rules 1.3.2 |
| HotshotLogistics.IntegrationTests | `5-Test/tests/HotshotLogistics.IntegrationTests/HotshotLogistics.IntegrationTests.csproj` | Test.Sdk 18.8.1, xunit.v3 3.2.2, FluentMigrator(.Runner.SqlServer) 8.0.1, Mvc.Testing 8.0.30 |
| HotshotLogistics.DbSetup | `7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj` | FluentMigrator.Runner 8.0.1, SqlClient 7.0.2, M.E.Logging.Console 10.0.11, System.CommandLine 2.0.11 |
| HotshotLogistics.DbSetup.Tests | `7-Deployment/DbSetup/HotshotLogistics.DbSetup.Tests/HotshotLogistics.DbSetup.Tests.csproj` | Test.Sdk 18.8.1, xunit.v3 3.2.2, SqlClient 7.0.2 |

Many `Microsoft.Extensions.*` references are already at 10.0.11 (multi-targeted
packages consumable from net8.0); the ASP.NET Core-aligned packages
(`Mvc.NewtonsoftJson`, `Mvc.Testing`, `TestHost`) are pinned to 8.0.30 and must move
to 10.0.x with the TFM.

### Facts discovered

- `Directory.Build.props` carries no TFM; it sets `TreatWarningsAsErrors`,
  `EnableNETAnalyzers`, `EnforceCodeStyleInBuild` (IDE0005/IDE1006 as errors),
  `GenerateDocumentationFile`, and `PlatformTarget ARM64` on macOS. New analyzer
  diagnostics on net10.0 surface as build errors and must be fixed in source, not
  suppressed.
- No `global.json` exists. Local SDKs: 8.0.427, 10.0.403, 11.0.100-rc.1. Without a
  pin, SDK resolution selects the highest installed SDK, which is the .NET 11 rc.
- No Dockerfiles exist; `7-Deployment/docker-compose.yml` runs SQL Server 2022 only.
  There are no container base images to change.
- No MAUI project and no EF Core (EF is prohibited); persistence is ADO.NET +
  FluentMigrator 8.0.1. `Swashbuckle` is already on major 10.
- Test stack: xUnit v3 (`xunit.v3` 3.2.2, `Microsoft.NET.Test.Sdk` 18.8.1,
  `xunit.runner.visualstudio` 3.1.5, `coverlet.collector` 10.0.1), FluentAssertions
  8.10.0, Moq 4.20.72, NetArchTest 1.3.2. Four test projects.
- `HotshotLogistics.Api.Tests` is **not** in `HotshotLogistics.sln` (the solution
  holds 11 projects); CI's find-based loop still restores, builds, and tests it.
- DB-backed tests skip via `DatabaseFactAttribute`
  (`5-Test/tests/HotshotLogistics.Tests/Utils/TestHelpers/DatabaseFactAttribute.cs`)
  unless `ConnectionStrings:DefaultConnection` (user secret) or
  `CONNECTIONSTRINGS__DEFAULTCONNECTION` is set. The CI test job runs without SQL
  Server, so those tests skip in CI.
- CI (`.github/workflows/dotnet-buid-test.yml`) pins `dotnet-version: '9.0.x'` in
  both the build job and the dependabot job; it builds every `*.csproj` in Release
  with `-warnaserror:IDE0005` and runs every `*.csproj` under `5-Test`.
  `API-deploy.yml` sets `DOTNET_VERSION: '9.0.x'`, deploys framework-dependent
  output, and runs migrations with the global tool
  `FluentMigrator.DotNet.Cli --version 3.4.0` against the
  `bin/Release/net8.0/HotshotLogistics.Data.dll` path.
- Terraform (`7-Deployment/Azure-deploy/solution/HotshotLogistics/main.tf`) pins
  `azurerm ~> 4.34.0`, `random ~> 3.0`, and AVM modules: web-serverfarm 0.7.0,
  web-site 0.17.2, appconfiguration store 0.1.0, sql-server 0.1.5, keyvault 0.10.0.
  There is no `required_version` block and **no .NET runtime version pin anywhere
  in the Terraform files** — the App Service/Function App runtime major is not
  managed in this tree (see Risks).
- `apm.yml` / `apm.lock.yaml` govern only the agent-skills package
  `dpalfery/kyber-weave` (deploys `.agents/skills/kyber-weave-docs`). They do **not**
  cover NuGet, npm, GitHub Actions, or Terraform dependencies.
- admin-dashboard: Next.js 15.5.4, React 19.1.0, `@microsoft/signalr` ^9.0.6,
  Playwright tests in `1-Presentation/admin-dashboard/tests/`, `package-lock.json`
  present, no `.nvmrc`. No CI workflow builds or tests the dashboard. The
  `dev`/`predev` scripts reference `certificates/*.pem`, which are not in the tree
  (locally generated only) — no secrets-ban issue.
- `RollForward LatestMajor` is set on MigrationRunner, Tests, IntegrationTests,
  DbSetup, and DbSetup.Tests; the Api (Web) has no `RollForward`, so a
  framework-dependent publish only runs where its TFM's runtime exists — the TFM
  change is what carries the deployment, not roll-forward.
- Hardcoded `net8.0` outside csproj files:
  - `7-Deployment/DbSetup/HotshotLogistics.DbSetup/MigrationInvoker.cs`,
    `RunMigrationsAsSubprocessAsync` — path segment `"net8.0"` to
    `MigrationRunner.dll`;
  - `.vscode/launch.json` — `bin/Debug/net8.0/HotshotLogistics.Api.dll`;
  - `.vscode/tasks.json` — `net8.0` path literals in scratch task commands.
- `.github/dependabot.yml` watches nuget and github-actions and deliberately ignores
  semver-major updates of `actions/setup-dotnet` — the SDK pin change is manual by
  design.

### Breaking-change sweep (self-gathered)

- **ASP.NET Core 10 official breaking changes**
  (learn.microsoft.com/aspnet/core/breaking-changes/10/overview, fetched
  2026-11-09): `WebHostBuilder`/`IWebHost` obsolete (repo uses minimal hosting —
  not exposed); `WithOpenApi` deprecation (repo uses Swashbuckle
  `AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI` in
  `1-Presentation/HotshotLogistics.Api/Program.cs` — not exposed); Razor runtime
  compilation (no Razor — not exposed); `IActionContextAccessor`,
  `IPNetwork`/`KnownNetworks`, `Microsoft.Extensions.ApiDescription.Client`,
  OpenAPI analyzers (none used — verified by grep). Behavioral items to watch:
  cookie-login redirects for API endpoints and suppressed exception diagnostics for
  `TryHandleAsync` handlers — the existing `AuthenticationTests` and
  `ExceptionHandlingMiddlewareTests` cover both behaviors.
- **Package majors with real call sites**: `Microsoft.AspNetCore.Mvc.NewtonsoftJson`
  8.0.30 → 10.0.x at `Program.cs` `.AddNewtonsoftJson(...))`;
  `Microsoft.AspNetCore.Mvc.Testing` / `Microsoft.AspNetCore.TestHost` 8.0.30 →
  10.0.x consumed by the `WebApplicationFactory<Program>` fixtures
  (`CustomWebApplicationFactory`, `TestingWebAppFactory`, `IntegrationTestBase`);
  `Microsoft.Identity.Web` 4.14.2 (current major, `AddMicrosoftIdentityWebApi` in
  `Program.cs`); `Microsoft.Azure.SignalR` 1.33.1 and
  `Microsoft.Azure.SignalR.Management` 1.33.x (1.33.1 is the current latest —
  self-gathered from NuGet; server SDK is referenced by Api/Application with hub
  usage in `2-Application/HotshotLogistics.Application/Services/SignalRClientWrapper.cs`
  via `IHubContext<RealtimeHub, IRealtimeHubClient>`);
  `Microsoft.Azure.AppConfiguration.AspNetCore` / `...Configuration.AzureAppConfig`
  8.6.0 is the current latest (self-gathered).
- **JSON casing sensitivity**: `.vscode/tasks.json` contains scratch experiments that
  once proved `MvcNewtonsoftJsonOptions` default casing behavior, so this repository
  has a history of caring about Newtonsoft serializer defaults across versions. The
  controller tests assert response JSON, which is the regression surface for any
  default-casing change between 8.x and 10.x.
- **Version anchors for execution day** (verify with `dotnet list package --outdated`
  / `npm outdated` at run time; self-gathered 2026-11-09): Microsoft.Data.SqlClient
  7.1.1 latest; FluentMigrator 8.0.1 is the latest library **and** the latest
  `FluentMigrator.DotNet.Cli` global tool (CI's 3.4.0 pin is far behind);
  `@microsoft/signalr` npm 10.0.11 latest; Next.js 15 reached EOL 2026-10-21 and
  Next.js 16 is the current stable line.
- **Test stack**: xUnit v3 and `Microsoft.NET.Test.Sdk` 18.x are current-generation
  and net10.0-compatible; no runner change is required.

### Documentation-gate results (worktree CLI, 2026-11-09)

Run by the conductor in the worktree:

- `kyber-weave docs validate . --merge-ready` — critical 0, error 3, warning 0,
  info 0:
  1. KW-DOC-SPEC-001 (error) — `6-Docs/admin-dashboard/AZURE_AD_SETUP.md` has no
     YAML frontmatter block.
  2. KW-DOC-LIFECYCLE-002 (error) — the todo `admin-dashboard-job-status-unresolved`
     is in `6-Docs/todo/` but `6-Docs/todo/README.md` does not link to it, so the
     todo inventory does not show it.
  3. KW-DOC-LIFECYCLE-003 (error) — this plan is still open in `6-Docs/plans/`; a
     merge must not carry open work.
- `kyber-weave docs drift .` — 0 findings, 0 critical, 0 error, 0 warning, 0 info.

The conductor's run of the same validate command at the base revision reports
exactly findings 1 and 2 and nothing else, so those two are pre-existing on the base
branch and are assigned to task T8; finding 3 is this plan itself while it is open
on its branch, and clears at closeout when the plan is archived.

## Decisions

| ID | Question | Answer / decision | Mitigating and supporting information | Status / approval provenance |
|---|---|---|---|---|
| D1 | How aggressive is the third-party dependency refresh? | (a) Latest stable, including major versions (previews/rc excluded per D3) | NuGet majors included (e.g. `Microsoft.AspNetCore.Mvc.NewtonsoftJson`/`Mvc.Testing`/`TestHost` → 10.0.x, `Microsoft.Data.SqlClient` → current stable, plus the other majors named in the breaking-change sweep); npm majors included (`next` 15.5.4 → 16.x, `@microsoft/signalr` → 10.0.11, React 19.x latest, others per `npm outdated`). Every major bump stays gated by the build gate, the four test suites, and the review council. Note: the user's answer offered `FluentMigrator.DotNet.Cli` → 8.0.1 as an example, but that tool pin lives inside `API-deploy.yml` (CI surface), so D2's exclusion governs it and it stays at 3.4.0. | ANSWERED — User decision 2026-11-09 |
| D2 | Scope of the refresh: .NET/NuGet only, or the full dependency surface? | The refresh covers the application's own dependencies only: every NuGet package across the solution and the admin-dashboard npm packages in `1-Presentation/admin-dashboard`. It excludes the CI workflow surface and the Terraform surface. | User's words: "all app packages not CI or terraform". The Terraform provider and module pins stay exactly as they are (`azurerm ~> 4.34.0`, `random ~> 3.0`, the five AVM module versions) — the Terraform task is dropped from this plan. The CI workflow surface stays untouched apart from the .NET SDK pin recorded in D5: no action version bumps, no tool version bumps, no other workflow edits. | ANSWERED — User decision 2026-11-09 |
| D3 | Are preview/rc packages and SDKs excluded? | (a) Exclude all preview and release-candidate package versions and the .NET 11 rc SDK; `global.json` carries `allowPrerelease: false`. | Keeps the LTS upgrade on stable, supported versions only; the locally installed 11.0.100-rc.1 SDK is never selected for this repository's builds. | ANSWERED — User decision 2026-11-09 |
| D4 | Is `apm.lock.yaml` regenerated as part of this change? | (a) Leave `apm.yml` and `apm.lock.yaml` unchanged. | Finding: `apm` governs only the agent-skills package `dpalfery/kyber-weave` (`apm.lock.yaml` deploys `.agents/skills/kyber-weave-docs`); it covers no product dependency, so it is outside this change. | ANSWERED — User decision 2026-11-09 |
| D5 | Is a `global.json` required, and which CI pins change? | Yes: add `global.json` at repository root with `sdk.version 10.0.403`, `rollForward latestFeature`, `allowPrerelease false`. The CI .NET SDK pin moves to 10.x in all three places it occurs — `dotnet-buid-test.yml` build job and dependabot job (`dotnet-version`), and `API-deploy.yml` (`DOTNET_VERSION`). Nothing else in `.github/workflows` changes: no action version bumps, no tool version bumps, no other workflow edits. | Without a pin, SDK resolution selects the locally installed `11.0.100-rc.1` preview, breaking local/CI determinism; `latestFeature` keeps the pin compatible with CI's `'10.0.x'`. Consequence of the pin move: net10.0 projects cannot be built by the 9.0.x SDK CI pins, so the pin move is mandatory and ships with the cutover in this same change. | ANSWERED — architect decision per task packet delegation (runtime and SDK pinning); CI SDK pin scope confirmed by User decision ("Move only the CI .NET SDK pin to 10.x") 2026-11-09 |
| D6 | Development mode for this plan. | `test-first` | The user has not opted out of test-first; the TFM cutover gets a real RED→GREEN guard test (T1/T2); the remaining tasks are explicitly no-new-test tasks whose verification is named in the Test contract. | ANSWERED — task packet `DEVELOPMENT_MODE: test-first` 2026-11-09 |
| D7 | Add `HotshotLogistics.Api.Tests` to `HotshotLogistics.sln` during the cutover? | Yes: add the project to the solution in T2. | The project exists on disk and CI's find loop builds/tests it, but it is invisible to `dotnet restore HotshotLogistics.sln` (root README onboarding step 2), so the net10.0 solution-build gate would not cover it. Small, reversible, verified by the same build. | ANSWERED — architect decision 2026-11-09, subject to the approve-and-execute gate |
| D8 | Target framework version. | `net10.0` for every project that targets a framework today (all 12). | The user chose ".net 10 latest" explicitly; not reopened. | ANSWERED — user request, verbatim, relayed by conductor 2026-11-09 |
| D9 | Does the `net8.0` build-output path literal in `API-deploy.yml`'s migration step move to `net10.0` with the cutover? | (a) Yes: the single `net8.0` path literal in the "Run database migrations" step of `.github/workflows/API-deploy.yml` moves to `net10.0` in the same change as the CI .NET SDK pin. | Mechanical consequence of D8: `API-deploy.yml` runs `dotnet fm migrate -a 4-Persistence/HotshotLogistics.Data/bin/Release/net8.0/HotshotLogistics.Data.dll`, and the `net8.0` build output ceases to exist after the cutover, so the step would otherwise fail with file-not-found. This is the only `.github/workflows` edit beyond the .NET SDK pins already covered by D5. The pinned `dotnet fm` CLI version (3.4.0) is unchanged and therefore may not load net10.0 migration assemblies at deploy time — an accepted, diagnosable out-of-scope consequence kept in Risk 5. | ANSWERED — User decision 2026-11-09 |
| D10 | Approve and execute this plan as presented? | Approved. Execute as presented. | The approve-and-execute gate stated: all 12 projects move from `net8.0` to `net10.0`; `global.json` pins SDK 10.0.403; NuGet and dashboard npm dependencies go to latest stable with major versions included; the CI .NET SDK pin moves to 10.x plus the single deploy path literal; the documentation updates and the two pre-existing documentation findings are cleared; declared scale `documentation-impact: extends-component` with no new component; impact components `Api`, `DbSetup`, `admin-dashboard`; development mode test-first; no infrastructure is created or applied, and no secret enters the repository. User's answer, verbatim: "Approve and execute". | ANSWERED — Approved by user 2026-11-09 |
| D11 | Wording correction to T8's `Depends on` bullet for the Arbiter's parser? | Corrected: the bullet now reads "…and this task's validation-clean check runs against T7's finished edits", naming only T7. | Changes one sentence's wording in T8's `Depends on` bullet and nothing else: T8's dependency (T7), file scope, acceptance criteria, the task graph, the declared scale (`extends-component`), the development mode (test-first) and every other task are untouched, and T2's file-scope label stays as it is (the Arbiter's "lists no files" report for T2 is a warning that only skips that task's file-scope check, not a defect). Reason: the Arbiter's parser reads the dependency ids appearing in the task text, so a task naming itself in its own section parsed as depending on itself (`T7, T8`) and could never satisfy the readiness gate. The correction preserves exactly what was approved. | ANSWERED — Approved by user 2026-11-09 (the approve-and-execute approval covers the plan's meaning; this correction preserves it) |

## Design harvest

| Target component | Target doc type | Document | Claim to revisit at closeout |
|---|---|---|---|
| Api | reference | `6-Docs/technology-stack.md` | "**Framework**: .NET 8 ASP.NET Core Web API…"; testing/build rows stay accurate |
| Api | onboarding | `6-Docs/api/onboarding.md` | ".NET 8 SDK" prerequisite line |
| Api | reference (devops) | `6-Docs/devops/ci-cd-pipelines.md` | The `setup-dotnet@v4 dotnet-version` and `DOTNET_VERSION` SDK-pin references move to '10.0.x' (D5) plus the determinism section; the `net8.0` migration-assembly path moves to `net10.0` (D9(a)); the FluentMigrator CLI version claim stays 3.4.0 (D2 exclusion) and the doc must keep matching it |
| Api | reference (devops) | `6-Docs/devops/deployment-architecture.md` | "targeting `net8.0`" claim; DbSetup ".NET 8 console tool" lines; SDK pin references; any DR register entry that restates the target framework |
| Api | readme (repo file) | root `README.md`, `1-Presentation/HotshotLogistics.Api/README.md` | "ASP.NET Core Web API (.NET 8)", ".NET 8 SDK" onboarding step |
| DbSetup | architecture | `6-Docs/dbsetup/architecture.md` | "DbSetup is a .NET 8 console tool built on `System.CommandLine`" |
| DbSetup | onboarding | `6-Docs/dbsetup/onboarding.md` | ".NET 8 SDK" prerequisite line |
| DbSetup | readme (repo file) | `7-Deployment/DbSetup/README.md` | "Cross-platform .NET 8 console tool…" |
| DbSetup | source | `7-Deployment/DbSetup/HotshotLogistics.DbSetup/MigrationInvoker.cs` | Not a doc, but the migration path string is a documented behavior: the `ci-cd-pipelines.md` and `dbsetup` docs describe migration invocation that hardcodes the TFM segment |
| admin-dashboard | reference | `6-Docs/technology-stack.md` | "Next.js 15, React 19", "SignalR client" rows (T5 upgrades them: Next 16, `@microsoft/signalr` 10) |
| admin-dashboard | reference | `6-Docs/admin-dashboard/AZURE_AD_SETUP.md` | Gains ontology-conformant YAML frontmatter (T8, clears KW-DOC-SPEC-001); content unchanged |
| admin-dashboard | index | `6-Docs/todo/README.md` | Todo inventory links `admin-dashboard-job-status-unresolved` (T8, clears KW-DOC-LIFECYCLE-002) — or the better resolution T8 finds and records |
| internal libraries | readme (repo file) | `0-Base/HotshotLogistics.Core/README.md`, `2-Application/HotshotLogistics.Application/README.md`, `3-Domain/HotshotLogistics.Domain/README.md`, `3-Domain/HotshotLogistics.Contracts/README.md` | ".NET 8 class library" lines → .NET 10 |
| platform | rule | `6-Docs/rules/aspnet-core-practices.md`, `6-Docs/rules/validation.md` | ".NET 8+ projects" phrasing — confirm it remains accurate or update to .NET 10 |

## Test contract (development-mode: test-first)

| Task | Test project or file | Runner command | Observable behavior | RED evidence required | GREEN acceptance |
|---|---|---|---|---|---|
| T1 (RED) | New `5-Test/tests/HotshotLogistics.Tests/Utils/Infrastructure/TargetFrameworkTests.cs` | `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --filter "FullyQualifiedName~TargetFrameworkTests"` | The Api assembly is compiled for `.NETCoreApp,Version=v10.0` (assert `TargetFrameworkAttribute` on `typeof(Program).Assembly`, per the repo's xUnit + FluentAssertions stack) | Failing run of the filter command on the current `net8.0` build, captured before any csproj edit | n/a (T2 carries GREEN) |
| T2 (GREEN) | Same class | Same filter command | Same | n/a | Filter command passes; `dotnet build HotshotLogistics.sln -c Release` succeeds with warnings-as-errors enabled |
| T3 | No new test — existing suites are the contract (explicit no-new-test task) | `dotnet test` per project, four commands below | All existing behavior unchanged on net10.0 with upgraded packages | n/a | All four test projects pass on net10.0; `dotnet list HotshotLogistics.sln package --outdated` shows nothing outstanding within the D1/D3 policy |
| T4 | No test surface — read-only verification | YAML parse + pin sweep below | The CI .NET SDK pin reads 10.x in all three occurrences and the migration step's path literal reads `net10.0`; no other workflow content changes (D2/D5/D9) | n/a | YAML parses; no `9.0.x` or `net8.0` remains under `.github/workflows`; the workflow diff shows only the three SDK-pin lines plus the single migration path literal |
| T5 | No new test — build/lint replace it; existing Playwright suite is the smoke | `npm run build` / `npm run lint` in `1-Presentation/admin-dashboard` | Dashboard builds and lints clean on upgraded deps | n/a | Build and lint pass; Playwright suite (`npm run test`) runs if a local stack is available, otherwise the limitation is recorded |
| T7 | No test surface — documentation gates | Conductor-run `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .` in the worktree | Docs assert .NET 10 truthfully | n/a | Both checks pass for the touched documents |
| T8 | No test surface — read-only verification | Worktree CLI docs checks below | The two pre-existing validation findings are cleared (KW-DOC-SPEC-001, KW-DOC-LIFECYCLE-002) | n/a | `kyber-weave docs validate . --merge-ready` reports neither finding — only KW-DOC-LIFECYCLE-003 for this open plan may remain; drift stays at 0 findings |

Key full test class names workers must keep green (guard classes):
`HotshotLogistics.Tests.Utils.Infrastructure.ArchitectureTests` (Clean Architecture,
NetArchTest), `HotshotLogistics.Tests.Utils.Infrastructure.ExceptionHandlingMiddlewareTests`,
`HotshotLogistics.Tests.Utils.Infrastructure.AuthorizationTests`,
`HotshotLogistics.Api.Tests.AuthenticationTests`.

## Tasks

### T1 — Target-framework guard test (RED)

- **Objective:** Add `TargetFrameworkTests` asserting the Api assembly targets
  `.NETCoreApp,Version=v10.0`, and capture it failing on the current net8.0 build.
- **Session:** `dotnet10-red`
- **Skills:** `test-dev`
- **Depends on:** none
- **Files:** new
  `5-Test/tests/HotshotLogistics.Tests/Utils/Infrastructure/TargetFrameworkTests.cs`
  (class `TargetFrameworkTests`; follow the namespace convention of its neighbours,
  e.g. `ArchitectureTests.cs`; assert via `FluentAssertions` per the test standard;
  do not pin the patch number — assert the v10 major).
- **Commands:** the filter command in the Test contract T1 row (expect FAIL — RED
  evidence).
- **Acceptance:** test exists, compiles, and its failing run against the unmodified
  net8.0 projects is recorded.

### T2 — net10.0 cutover, SDK pin, path literals (GREEN)

- **Objective:** Move every project that targets a framework to `net10.0`, pin the
  SDK, and update every hardcoded `net8.0` path literal.
- **Session:** `dotnet10-core`
- **Skills:** `csharp-dev`
- **Depends on:** T1
- **Files (path — symbol/setting):**
  - all 12 `*.csproj` files listed in the inventory — `TargetFramework` (`net8.0` →
    `net10.0`); do not touch `RollForward`, `UserSecretsId`, `InternalsVisibleTo`,
    or custom output paths;
  - new `global.json` at repository root — `sdk.version 10.0.403`,
    `rollForward latestFeature`, `allowPrerelease false` (D5);
  - `HotshotLogistics.sln` — add project entry for
    `5-Test/HotshotLogistics.Api.Tests/HotshotLogistics.Api.Tests.csproj` (D7);
  - `7-Deployment/DbSetup/HotshotLogistics.DbSetup/MigrationInvoker.cs` —
    `RunMigrationsAsSubprocessAsync`: `"net8.0"` path segment → `"net10.0"`;
  - `.vscode/launch.json` — `program` path `bin/Debug/net8.0/...` → `net10.0`;
  - `.vscode/tasks.json` — `net8.0` path literals → `net10.0`.
- **Commands:**
  - `dotnet build HotshotLogistics.sln --configuration Release`
  - the T1 filter command (expect PASS — GREEN)
- **Acceptance:** solution (now 12 projects) builds Release with
  warnings-as-errors; guard test green; no `net8.0`/`net8` string remains in csproj,
  `global.json` present.

### T3 — NuGet dependency refresh and compile fixes

- **Objective:** Upgrade every `PackageReference` across the 12 projects to the
  latest stable version per D1/D3 (previews excluded), and fix every resulting
  compile or analyzer break at the call sites named in the breaking-change sweep.
- **Session:** `dotnet10-core` (same worker as T2)
- **Skills:** `csharp-dev`
- **Depends on:** T2
- **Files:** the 12 `*.csproj` — every `PackageReference` `Version`; plus whatever
  source files break (expected candidates per the sweep: `Program.cs`
  `AddNewtonsoftJson` serializer settings, `WebApplicationFactory` fixtures in
  `5-Test/tests/HotshotLogistics.Tests/Utils/TestHelpers/` and
  `5-Test/tests/HotshotLogistics.IntegrationTests/`, `Program.cs`
  `AddMicrosoftIdentityWebApi`). Known anchors: `Microsoft.AspNetCore.Mvc.NewtonsoftJson`,
  `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.TestHost` → 10.0.x;
  `Microsoft.Data.SqlClient` → 7.1.x; FluentMigrator(.Runner[.SqlServer]) stays
  8.0.1 (current); `Microsoft.Azure.SignalR`/`.Management` → current 1.33.x;
  App Configuration packages stay 8.6.0 unless a newer stable exists; the rest per
  `dotnet list package --outdated`.
- **Commands:**
  - `dotnet list HotshotLogistics.sln package --outdated`
  - `dotnet build HotshotLogistics.sln --configuration Release`
  - `dotnet test 5-Test/HotshotLogistics.Api.Tests/HotshotLogistics.Api.Tests.csproj --configuration Release`
  - `dotnet test 5-Test/tests/HotshotLogistics.Tests/HotshotLogistics.Tests.csproj --configuration Release`
  - `dotnet test 5-Test/tests/HotshotLogistics.IntegrationTests/HotshotLogistics.IntegrationTests.csproj --configuration Release`
  - `dotnet test 7-Deployment/DbSetup/HotshotLogistics.DbSetup.Tests/HotshotLogistics.DbSetup.Tests.csproj --configuration Release`
  - Optional full DB coverage: `docker compose -f 7-Deployment/docker-compose.yml up -d`
    and set `CONNECTIONSTRINGS__DEFAULTCONNECTION` so `DatabaseFact` tests run
    instead of skipping.
- **Acceptance:** zero outstanding packages within policy; solution builds clean;
  all four test projects pass on net10.0; guard classes listed in the Test contract
  stay green; no `NoWarn`/suppression additions without a recorded reason
  (Directory.Build.props owns analyzer policy).

### T4 — CI .NET SDK pin and migration path literal

- **Objective:** Move the CI .NET SDK pin to 10.x (D5) and the deploy migration
  step's `net8.0` build-output path literal to `net10.0` (D9). These are the only
  `.github/workflows` edits in this plan (D2): no action version bumps, no tool
  version bumps, no other workflow edits. The `FluentMigrator.DotNet.Cli` 3.4.0
  tool pin and every action pin stay exactly as they are.
- **Session:** `ci-pins`
- **Skills:** `github-devops`
- **Depends on:** T2 — the migration path literal consumes the cutover's TFM value,
  and the SDK pin and cutover land in the same change (D5).
- **Files:**
  - `.github/workflows/dotnet-buid-test.yml` — `dotnet-version: '9.0.x'` →
    `'10.0.x'` (build job and dependabot job, per D5);
  - `.github/workflows/API-deploy.yml` — `env.DOTNET_VERSION` `'9.0.x'` →
    `'10.0.x'` (per D5), and the "Run database migrations" step's
    `bin/Release/net8.0/HotshotLogistics.Data.dll` → `net10.0` (per D9);
  - `.github/workflows/terraform-plan.yml`, `.github/workflows/terraform-apply.yml`,
    `.github/dependabot.yml` — untouched.
- **Commands (verification):**
  - `python3 -c "import yaml; [yaml.safe_load(open(f'.github/workflows/{f}')) for f in ['dotnet-buid-test.yml','API-deploy.yml','terraform-plan.yml','terraform-apply.yml']]; print('yaml ok')"`
  - sweep: `grep -rn "9\.0\.x\|net8\.0" .github/workflows` must return nothing.
- **Acceptance:** YAML parses; all three SDK-pin occurrences read 10.x; the migration
  path literal reads `net10.0`; the workflow diff contains nothing but the three
  SDK-pin lines and the single path literal; CI runs green on the PR.

### T5 — admin-dashboard npm refresh

- **Objective:** Upgrade dashboard dependencies per D1/D3: majors included
  (`next` 15.5.4 → 16.x, `@microsoft/signalr` → 10.0.11, React 19.x latest, others
  per `npm outdated`), previews excluded; regenerate the lockfile.
- **Session:** `npm-dashboard`
- **Skills:** `typescript-dev`
- **Depends on:** none
- **Files:** `1-Presentation/admin-dashboard/package.json` — `dependencies`,
  `devDependencies`; `1-Presentation/admin-dashboard/package-lock.json` —
  regenerated. Fix any compile/lint breaks in `src/` and `tests/` that the majors
  introduce (Next 16 upgrade guide is the reference for breaking changes).
- **Commands:** in `1-Presentation/admin-dashboard`:
  - `npm install`
  - `npm run lint`
  - `npm run build`
  - `npm run test:install && npm run test` (Playwright smoke; if the required local
    stack/certificates are unavailable, record that limitation explicitly)
- **Acceptance:** install, lint, and build clean on the upgraded versions;
  Playwright run recorded or its unavailability documented.

### T7 — Documentation closeout

- **Objective:** Update every documented stack fact per the Design harvest table so
  no governed doc or README asserts .NET 8 or the old pins.
- **Session:** `docs-closeout`
- **Skills:** `app-docs-standard`, `kyber-docs-authoring`
- **Depends on:** T3, T4, T5
- **Files:** every document in the Design harvest table (paths there). Also run a
  final sweep for `net8` / `.NET 8` across `6-Docs/` (excluding `6-Docs/archive/`,
  which is historical and never updated) and the repo READMEs, and reconcile
  anything found.
- **Commands:** none beyond the docs gates; the conductor runs
  `kyber-weave docs validate . --merge-ready` and `kyber-weave docs drift .` in the
  worktree (the kw-docs MCP server is bound to the canonical checkout and cannot
  see this worktree).
- **Acceptance:** all harvest rows updated; docs validate and drift pass for the
  touched documents; `technology-stack.md` states .NET 10 and the new frontend
  versions.

### T8 — Clear the pre-existing documentation validation findings

- **Objective:** Clear the two documentation-validation errors the base revision
  already carries, so the end-of-run review council sees a clean tree:
  KW-DOC-SPEC-001 (`6-Docs/admin-dashboard/AZURE_AD_SETUP.md` — no YAML
  frontmatter) and KW-DOC-LIFECYCLE-002 (`6-Docs/todo/README.md` — the todo
  `admin-dashboard-job-status-unresolved` is not linked in the inventory).
- **Session:** `docs-findings`
- **Skills:** `kyber-docs-authoring`, `app-docs-standard`
- **Depends on:** T7 — both tasks are broad `6-Docs/` edits, so they are serialized
  to keep their file scopes from overlapping, and this task's validation-clean
  check runs against T7's finished edits.
- **Files:**
  - `6-Docs/admin-dashboard/AZURE_AD_SETUP.md` — add the YAML frontmatter block the
    ontology requires for its document type (`6-Docs/documentation-ontology.md`),
    following the conventions the sibling documents in `6-Docs/admin-dashboard/`
    already use (`id`, `title`, `doc-type`, `status`, `component`, `owner`,
    `last-reviewed`, plus `source-root` or `technology` where the doc-type requires
    them); document content is otherwise unchanged;
  - `6-Docs/todo/README.md` — add the missing inventory row for the todo
    `admin-dashboard-job-status-unresolved`, in the shape the existing rows in that
    file use. Read the todo first: if it shows a better resolution (for example the
    work is already finished and the todo belongs in `6-Docs/archive/todo/`), take
    that resolution instead and record which resolution was taken in the task
    report.
- **Commands (verification, run in the worktree):**
  - `kyber-weave docs validate . --merge-ready`
  - `kyber-weave docs drift .`
- **Acceptance:** validate reports neither KW-DOC-SPEC-001 nor KW-DOC-LIFECYCLE-002 —
  only KW-DOC-LIFECYCLE-003 for this open plan may remain, and it clears when
  closeout archives the plan; drift stays at 0 findings; the frontmatter is
  ontology-conformant and the todo inventory resolution (link or archive) is
  recorded.

## Dependency graph

```
T1 (RED, test-dev) ──► T2 (csharp-dev) ──► T3 (csharp-dev) ──┐
                          │                                   ├──► T7 (docs) ──► T8 (docs findings)
                          └──► T4 (github-devops) ─────────────┤
T5 (typescript-dev) ──────────────────────────────────────────┘
```

- T1 → T2 → T3 are strictly sequential (RED before its GREEN; T3 touches the same
  csproj scope).
- T4 depends on T2: its migration path literal consumes the cutover's TFM value
  (D9), and the SDK pin and cutover must land in the same change (D5).
- T5 is an independent surface and may run concurrently with T2–T4.
- T8 depends on T7: both are broad `6-Docs/` tasks, so they are serialized to keep
  their file scopes from overlapping, and T8's validation-clean check runs against
  T7's finished edits.
- T7/T8 run last; T1 and T5 can be in flight before the cutover completes (within
  the harness concurrency cap, default 4 per `.kyber-weave/kyber-weave.yml`).

## Risks

1. **New analyzer diagnostics on net10.0 become build errors** (`TreatWarningsAsErrors`
   + `AnalysisLevel latest`). Mitigation: fix source at the call site; any `NoWarn`
   addition must be recorded in the task report with its reason.
2. **Newtonsoft.Json default-casing drift** between Mvc.NewtonsoftJson 8.0.30 and
   10.0.x. Mitigation: controller tests assert response JSON; run the full Api and
   integration suites, not just a filtered subset.
3. **Behavioral ASP.NET Core 10 changes** (cookie auth redirects, exception-handler
   diagnostics). Mitigation: `AuthenticationTests`, `AuthorizationTests`,
   `ExceptionHandlingMiddlewareTests` must stay green unmodified.
4. **App Service runtime major is not managed in this tree.** No Terraform pin sets
   a .NET runtime version; the framework-dependent publish requires the host to
   offer .NET 10. After merge, the App Service/Function App `.NET` runtime setting
   must be moved to .NET 10 out-of-band (portal/host configuration) — flagged as a
   deployment risk, not a code change, because the hosting model itself is
   unresolved (DR2) and infrastructure changes need explicit approval.
5. **FluentMigrator.DotNet.Cli stays at 3.4.0** in `API-deploy.yml` (D2: no tool
   version bumps in the CI surface). The 3.4.0 global tool predates .NET 10 and may
   fail to load or execute the net10.0 migration assembly at deploy time. Mitigation:
   deliberate out-of-scope by user decision 2026-11-09; the failure mode is recorded
   here so it is diagnosable, and a follow-up change can bump the pin (8.0.1 is the
   current tool version) if the deploy migration step fails after merge.
6. **Next.js 16 major** (approved by D1(a)) is the largest third-party jump.
   Mitigation: follow the official v16 upgrade guide; Playwright suite plus
   build/lint gate it.
7. **The deploy migration step's path exposure is narrowed but not eliminated by
   D9(a).** The `net8.0` build-output path literal moves to `net10.0` with the
   cutover, so the step's remaining exposure is the unchanged 3.4.0 `dotnet fm`
   CLI tool (Risk 5) — an accepted, diagnosable, out-of-scope consequence of D2.
8. **Preview SDK selection**: without the `global.json` pin the locally installed
   `11.0.100-rc.1` wins SDK resolution. Mitigation: D5's `global.json` with
   `allowPrerelease: false` ships in the same change as the TFM flip (T2).

## Out-of-scope boundaries

- No hosting-model resolution (DR2 stays unresolved); no App Service vs Function
  App decision.
- No infrastructure changes at all: the Terraform provider and module pins stay
  exactly as they are (`azurerm ~> 4.34.0`, `random ~> 3.0`, the five AVM module
  versions) per D2; no Pulumi, Bicep, or Ansible; no local `terraform apply`/
  `destroy`; no Azure CLI reads or writes.
- CI workflow surface beyond the .NET SDK pin (D5): no action version bumps
  (`actions/checkout`, `actions/setup-dotnet`, `github/codeql-action`,
  `trufflesecurity/trufflehog`, `azure/login`, `azure/webapps-deploy`,
  `hashicorp/setup-terraform`), no `FluentMigrator.DotNet.Cli` tool bump (stays
  3.4.0), no `dependabot.yml` edits — per D2 and the user's "Move only the CI .NET
  SDK pin to 10.x" clarification. The single in-scope exception is the deploy
  migration step's `net8.0` path literal, which moves to `net10.0` with the cutover
  per D9(a).
- No Moq→NSubstitute migration (the test standard prefers NSubstitute; the projects
  use Moq — a separate decision, not part of this upgrade).
- No new test frameworks, no mocking-library swap, no coverage-threshold change.
- `apm.yml`/`apm.lock.yaml` stay unchanged per D4 (user decision).
- No secrets, connection strings, or `.env` files introduced anywhere; user-secrets
  IDs in csproj files are carried over untouched.
- Clean Architecture layering (Contracts = interfaces only, Contracts.Models = DTOs
  only, Domain = invariants, Application `Services` = application services) survives
  unchanged; `ArchitectureTests` enforce it.

## Verification gates

1. **Build gate:** `dotnet build HotshotLogistics.sln --configuration Release`
   succeeds with warnings-as-errors on net10.0 (all 12 projects, including
   Api.Tests once added to the solution per D7).
2. **Test gate:** all four test projects pass per the Test contract commands
   (`HotshotLogistics.Api.Tests`, `HotshotLogistics.Tests`,
   `HotshotLogistics.IntegrationTests`, `HotshotLogistics.DbSetup.Tests`), with
   SQL Server backed via `7-Deployment/docker-compose.yml` where available so
   `DatabaseFact` tests run rather than skip.
3. **Dependency gate:** `dotnet list HotshotLogistics.sln package --outdated` shows
   nothing outstanding within the D1/D3 policy, and `npm outdated` in
   `1-Presentation/admin-dashboard` shows nothing outstanding within the same
   policy; the CI and Terraform surfaces are exempt per D2.
4. **Docs gates:** `kyber-weave docs validate . --merge-ready` and
   `kyber-weave docs drift .` run in the worktree (kw-docs MCP is bound to the
   canonical checkout and cannot see worktree plans). Baseline recorded 2026-11-09:
   drift passes at 0 findings; validate reports critical 0, error 3, warning 0 —
   KW-DOC-SPEC-001 (`6-Docs/admin-dashboard/AZURE_AD_SETUP.md`) and
   KW-DOC-LIFECYCLE-002 (`6-Docs/todo/README.md`), both pre-existing on the base
   revision and cleared by T8, plus KW-DOC-LIFECYCLE-003 for this plan while it is
   open on its branch, which clears when closeout archives it. The gate for this
   run: after T8 and the archive step, validate reports 0 errors and drift reports
   0 findings.
5. **CI gate:** the PR's full workflow set green (build/test, CodeQL, TruffleHog,
   terraform plan, and the deploy workflow's migration step on merge).

## Review

The end-of-run review council governs review, deriving its invocation from the
`code-review` skill (measured against the change with `--base`, admitted to the
full-suite gates with `--council`); this plan deliberately does not carry the command.
Each developer task additionally ends with its own `task-gate` run over the files
that task changed; a failing gate returns to the same worker. The council runs
against the gate-4-clean documentation tree that T8 delivers.

## docs-dev closeout

T7 executes the Design harvest: every listed document and README stops asserting
.NET 8 and the old pins, `technology-stack.md` records .NET 10 and the new
frontend versions (T5 always runs), and the final `net8`/`.NET 8` sweep across
`6-Docs/` (minus `archive/`) and the READMEs comes back clean. `catalog.md` review
dates for `Api` and `DbSetup` are bumped when their rows are touched. T8's
conformance fixes (`6-Docs/admin-dashboard/AZURE_AD_SETUP.md` frontmatter, the
`6-Docs/todo/README.md` inventory row) are part of the same closeout, so
`kyber-weave docs validate . --merge-ready` reports 0 errors once this plan itself
is archived. The conductor runs the two documentation checks in the worktree to
admit closeout.
