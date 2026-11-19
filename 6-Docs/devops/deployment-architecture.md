---
id: devops/deployment-architecture
title: Deployment architecture
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-11-14
---

# Deployment architecture

How the Hotshot Logistics platform is deployed as this repository actually implements it:
the Terraform under `7-Deployment/Azure-deploy/`, the two GitHub Actions workflows under
`.github/workflows/` (`ci.yml` and `release.yml`), the CI helper module under `7-Deployment/ci/`,
the local Docker Compose file, and the DbSetup CLI.

This document also hosts the **canonical drift register** for this repository — the
[Canonical drift register](#canonical-drift-register) section below. Other documents link
to that section instead of restating its items.

Every statement below was read from the source files named in the same subsection. Where the
code and the documentation disagree, both are reported and the disagreement is recorded in the
register rather than resolved here.

## Sources

| Area | Files read |
| --- | --- |
| CI/CD | `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `7-Deployment/ci/ci_helpers.py`, `.github/dependabot.yml` |
| Infrastructure as code | `7-Deployment/Azure-deploy/solution/launchpad/{main,variables,outputs}.tf`, `7-Deployment/Azure-deploy/solution/HotshotLogistics/{main,variables,outputs}.tf`, `7-Deployment/Azure-deploy/environments/dev/hotshot.tfvars` |
| Local dependencies | `7-Deployment/docker-compose.yml` |
| Provisioning tool | `7-Deployment/DbSetup/HotshotLogistics.DbSetup/Program.cs`, `7-Deployment/DbSetup/HotshotLogistics.DbSetup/MigrationInvoker.cs` |
| Application | `1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj`, `1-Presentation/HotshotLogistics.Api/Program.cs` |

## Environments

| Environment | Where the code defines it | Provisioning inputs |
| --- | --- | --- |
| `dev` | Terraform variable default (`environment = "dev"` in `variables.tf`); `workflow_dispatch` default in `release.yml` | `TF_VAR_*` env vars in `release.yml`; `7-Deployment/Azure-deploy/environments/dev/hotshot.tfvars` on disk |
| `staging` | `workflow_dispatch` choice option in `release.yml` only | `TF_VAR_environment` and `TF_VAR_resource_group_name` in `release.yml` (no `.tfvars` or separate state key) |
| `prod` | `workflow_dispatch` choice option in `release.yml` only | `TF_VAR_environment` and `TF_VAR_resource_group_name` in `release.yml` (no `.tfvars` or separate state key) |

The Terraform `environment` variable is a plain string whose only effect in the
`HotshotLogistics` solution is to prefix the six resource names built from it. Resource tags
come from the separate `tags` variable, a `map(string)` that defaults in `variables.tf`
independently of `environment`, so `environment` never reaches a tag. There is no per-environment
module and no separate state key. `staging` and `prod` exist as `release.yml` dispatch choices,
but no per-environment `.tfvars` file exists in the tree — see DR5 in the register.

## Hosting model

**The hosting model is unresolved.** The documentation and the infrastructure disagree, and
this document does not pick a winner. The conflict is recorded as DR2 in the register.

| Source | What it says |
| --- | --- |
| [technology-stack.md](../technology-stack.md) | "**Framework**: .NET 10 ASP.NET Core Web API (Azure App Service / Function App — see 'Hosting model')"; "**Cloud**: Azure (App Service / Function App, SQL Server, App Configuration, Key Vault)". Its "Hosting model" section states that the hosting model is "**unresolved**", names the actual deployment path, and points back at DR2. |
| [api/architecture.md](../api/architecture.md) | "`HotshotLogistics.Api` is an ASP.NET Core Web API. Its hosting model is unresolved — see the canonical drift register (DR2) in deployment architecture; the actual deployment path is an Azure App Service plan / Function App provisioned by Terraform, deployed by `.github/workflows/release.yml` (`deploy-api` job)." |
| [project-overview.md](../project-overview.md) | "ASP.NET Core Web API (hosting model unresolved — see DR2), SQL Server via native ADO.NET, Clean Architecture." |
| [rules/file-organization.md](../rules/file-organization.md) | "API controllers and endpoints" — the hosting parenthetical was dropped. |
| `7-Deployment/Azure-deploy/solution/HotshotLogistics/main.tf` | `module "function_app"` — AVM `Azure/avm-res-web-site/azurerm` v0.17.2, `kind = "functionapp"` |
| `.github/workflows/release.yml` | `deploy-api` job uses `azure/webapps-deploy` (`v3.0.8`), in a step named "Deploy to Azure App Service", with `startup-command: "dotnet HotshotLogistics.Api.dll"` |

Text quoted from another document is reproduced without that document's internal link markup.

[technology-stack.md](../technology-stack.md), [api/architecture.md](../api/architecture.md),
[project-overview.md](../project-overview.md), [rules/file-organization.md](../rules/file-organization.md),
the root `README.md`, and `1-Presentation/HotshotLogistics.Api/README.md` no longer assert Azure
Container Apps; each states that the hosting model is unresolved (pointing at DR2) and names the
actual Terraform + `.github/workflows/release.yml` deployment path (except
[rules/file-organization.md](../rules/file-organization.md), which drops the hosting parenthetical). See the
[register reconciliation note](#register-reconciliation-note) below.

No Container Apps resource is declared in any Terraform file, and no workflow step builds a
container image or deploys to Container Apps. The API project is `Microsoft.NET.Sdk.Web`
targeting `net10.0`; it carries no `host.json` and no Azure Functions worker package. It is
published with `dotnet publish` and started as a plain ASP.NET Core process, which is what the
`dotnet HotshotLogistics.Api.dll` startup command implies.

## Resource topology

### launchpad — remote state bootstrap

`7-Deployment/Azure-deploy/solution/launchpad/` provisions the Terraform remote state:

| Resource | Name | Settings |
| --- | --- | --- |
| Resource group | `rg-launchpad-dev-eastus` | `location = "East US"`; tags `Environment`, `Project`, `ManagedBy` |
| Storage account | `hotshotlogisticsstate` | `Standard`, `LRS` |

`variables.tf` declares `location` and `environment`, but `main.tf` hardcodes `East US` and
`rg-launchpad-dev-eastus` rather than referencing them.

No workflow invokes the launchpad solution. `.github/workflows/ci.yml` runs
`terraform init -backend=false -input=false`, while `.github/workflows/release.yml` sets
`working-directory: 7-Deployment/Azure-deploy/solution/HotshotLogistics`, so the state
backend has to be bootstrapped out of band before `release.yml` can initialize `azurerm`.

### HotshotLogistics — workload solution

`7-Deployment/Azure-deploy/solution/HotshotLogistics/`

- **Backend:** `azurerm` — resource group `rg-launchpad-dev-eastus`, storage account
  `hotshotlogisticsstate`, container `tfstate`, state key `hotshotlogistics.tfstate`.
- **Providers:** `hashicorp/azurerm ~> 4.34.0`, `hashicorp/random ~> 3.0`.

| Terraform address | Resource | Name for `dev` | Module source | Notable settings |
| --- | --- | --- | --- | --- |
| `azurerm_resource_group.rg` | Resource group | `rg-hotshot-logistics-dev-eastus2` in `release.yml` (`environments/dev/hotshot.tfvars` specifies `rg-hotshot-logistics-dev-eastus`; see DR3) | — | `East US 2` in `release.yml`; `East US` in `hotshot.tfvars` |
| `module.app_service_plan` | App Service plan | `dev-hotshot-logistics-plan` | `Azure/avm-res-web-serverfarm/azurerm` v0.7.0 | `sku_name = "B1"`, `os_type = "Linux"`, zone balancing disabled |
| `azurerm_storage_account.storage` | Storage account | `devhotshotstorage` | — | `Standard`, `LRS`; backs the Function App |
| `module.function_app` | Function App | `dev-hotshot-logistics-function` | `Azure/avm-res-web-site/azurerm` v0.17.2 | `kind = "functionapp"`, Linux, on the B1 plan above |
| `module.app_configuration` | App Configuration store | `dev-hotshot-logistics-config` | `Azure/avm-res-appconfiguration-configurationstore/azure` v0.1.0 | — |
| `module.sql_server` | SQL Server | `dev-hotshot-logistics-sql` | `Azure/avm-res-sql-server/azurerm` v0.1.5 | `server_version = "12.0"`, `administrator_login` default `sqladmin`, public network access enabled |
| `module.sql_database` | SQL database | `hotshot-logistics-db` | `Azure/avm-res-sql-server/azurerm//modules/database` v0.1.5 | `sku_name = "GP_S_Gen5_1"`, `min_capacity = 0.5`, auto-pause delay 60 minutes |
| `module.key_vault` | Key Vault | `dev-hotshot-log-kv-01` | `Azure/avm-res-keyvault-vault/azurerm` v0.10.0 | `sku_name = "standard"`; two secrets, listed below |
| `random_password.sql_password` | Generated password | — | — | length 16, special characters enabled |

Two details of the SQL database module are worth stating because they are easy to misread from
the variable names. The `compute_model = "Serverless"` line in the module block is commented
out, so the database is created with the General Purpose `GP_S_Gen5_1` SKU. The `min_capacity`
and `auto_pause_delay_in_minutes` settings that would apply to a serverless tier are present
in the module block, and the `max_capacity` variable is declared and set in the tfvars file but
is never passed to the module.

The `administrator_login_password` variable is declared `sensitive` with no default. The
tfvars file omits it and points at `TF_VAR_administrator_login_password`.
`.github/workflows/release.yml` exports `TF_VAR_administrator_login_password` from
`secrets.SQL_ADMIN_PASSWORD` (and fails early if `SQL_ADMIN_PASSWORD`, `AZURE_AD_CLIENT_ID`, or
`AZURE_AD_CLIENT_SECRET` is unset).

### Key Vault secret names

| Secret name | Content type | Source |
| --- | --- | --- |
| `sql-connection-string` | `text/plain` | Assembled in Terraform from the SQL Server fully qualified domain name, the database name, the `sqladmin` login, and the generated password |
| `sql-admin-password` | `text/plain` | The generated password |

No secret value is recorded in this document, in the tfvars files, or anywhere else in the
repository. The `sql-connection-string` value is written into the vault by Terraform, and
`.github/workflows/release.yml` (`deploy-api`) reads it back out at deploy time, masks it via
`::add-mask::`, and passes it via `env:` (`CONNECTION_STRING`) to run migrations. See the
[security guidelines](../security.md) for the repository-wide rules on secret handling.

### Workflow-to-Terraform name reconciliation

`.github/workflows/release.yml` derives its targets from `inputs.environment || 'dev'` rather
than from Terraform outputs, so both sides must agree by convention:

| Target | Name used by `release.yml` | Name provisioned by Terraform for `dev` | Match |
| --- | --- | --- | --- |
| App | `<environment>-hotshot-logistics-function` | `dev-hotshot-logistics-function` | yes |
| Key Vault | `<environment>-hotshot-log-kv-01` | `dev-hotshot-log-kv-01` | yes |

This table records observed naming facts about the current code. It does not add or change an
entry in the canonical drift register below.

## Workflow chain

```mermaid
flowchart TD
  pr["pull_request / push (main) / schedule / workflow_dispatch"]
  ci["CI (.github/workflows/ci.yml)<br/>changes -> backend, dashboard, dashboard-e2e, docs-gate, codeql, secret-scanning, terraform-validate -> ci-summary"]
  rel["Release (.github/workflows/release.yml)<br/>triggered by successful CI push on main, v* tag, or workflow_dispatch"]
  gate["release-gate<br/>classify changed paths via 7-Deployment/ci/ci_helpers.py"]
  tf["terraform<br/>Terraform Plan & Apply (same runner, no tfplan artifact upload)"]
  api["deploy-api<br/>App Service deploy + Key Vault lookup + dotnet fm migrate"]
  azure["Azure resources<br/>rg-hotshot-logistics-<env>-eastus2"]

  pr --> ci
  ci -->|workflow_run success on main push| rel
  rel --> gate
  gate -->|deploy_infra=true| tf
  gate -->|deploy_api=true & infra skipped or succeeded| api
  tf --> api
  tf --> azure
  api --> azure
```

See [ci-cd-pipelines.md](ci-cd-pipelines.md) for the complete job-by-job breakdown, triggers,
SHA-pinned action inventory, environment variables, and secrets for both `.github/workflows/ci.yml`
and `.github/workflows/release.yml`.

### Dependency updates

`.github/dependabot.yml` covers two ecosystems, both on a weekly Monday 09:00 schedule: NuGet
across the repository root, and GitHub Actions. Major version updates to `actions/setup-dotnet`
are ignored.

## Local SQL Server with Docker Compose

`7-Deployment/docker-compose.yml` defines one service:

| Setting | Value |
| --- | --- |
| Image | `mcr.microsoft.com/mssql/server:2022-latest` |
| Container name | `hotshot_sqlserver` |
| Ports | `1433:1433` |
| Volume | `hotshot_sqlserver_data` mounted at `/var/opt/mssql` |
| Environment | `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` taken from `SQL_SA_PASSWORD`, `MSSQL_PID=Developer` |
| Restart policy | `unless-stopped` |

Its role is to provide a local SQL Server 2022 instance for development and integration
testing. The `SQL_SA_PASSWORD` value is supplied by the caller from the environment; no value
is stored in the repository. The file provisions the database engine only. It does not create
the application database, the application login, or the schema — creating those is the job of
the DbSetup CLI and the FluentMigrator migrations.

The file also declares `version: '3.8'`, which the Compose specification marks obsolete.

## DbSetup CLI role

DbSetup (`7-Deployment/DbSetup/`) is a .NET 10 console tool that provisions SQL Server: it
creates the database, the application login and user. Its migration half is inert — the CLI
never invokes `MigrationInvoker`, so it applies no FluentMigrator migrations of its own (see
DR1). It is a **provisioning tool, not a runtime service**. It is not deployed, has no host, and
is not started by any pipeline.

Its source-root README, [`7-Deployment/DbSetup/README.md`](../../7-Deployment/DbSetup/README.md),
states that boundary only in part. Its `**Boundary:**` line reads "provisioning only. Not a
runtime service. Do not embed connection strings or passwords in this repository." and matches
the boundary above. Its opening sentence used to describe the tool as one that "creates the
SQL Server database and application login, then applies FluentMigrator migrations" — the DR1
claim, which the code does not support. That sentence was corrected on 2026-10-20, within this
delivery; DR1 in the register names that file as a location where the claim was recorded, and
the [register reconciliation note](#register-reconciliation-note) below records the correction.
The authoritative statements of the migration-inertness fact are
[dbsetup/architecture.md](../dbsetup/architecture.md) and
[dbsetup/onboarding.md](../dbsetup/onboarding.md).

Two facts about how it is used today:

1. **CI executes DbSetup against an ephemeral container; release deploys run `dotnet fm migrate`.**
   `.github/workflows/ci.yml` (`backend` job) invokes `HotshotLogistics.DbSetup` (`--non-interactive`)
   against an ephemeral SQL Server 2022 container, runs `4-Persistence/MigrationRunner`, and executes
   all four test projects including `HotshotLogistics.DbSetup.Tests`. By contrast,
   `.github/workflows/release.yml` (`deploy-api`) builds `4-Persistence/HotshotLogistics.Data` and
   runs `FluentMigrator.DotNet.Cli` (`dotnet fm migrate`) directly against Azure SQL.
2. **The tool's own migration path is currently unreachable.** `Program.cs` composes
   `ArgumentParser`, `EnvironmentManager`, `PasswordManager`, and `SqlServerProvisioner`. The
   `MigrationInvoker` step is a `// TODO` comment, and `MigrationInvoker.RunMigrationsAsync` is
   never called. The class is implemented but dead. See DR1 in the register.

The live half of DbSetup is therefore database and login provisioning; its migration half is
inert. See [dbsetup/architecture.md](../dbsetup/architecture.md) for the intended composition
and [dbsetup/onboarding.md](../dbsetup/onboarding.md) for how to run the tool.

## Canonical drift register

This section is the single source of truth for deployment drift for this repository. Other
documents link here rather than restating the items.

- **DR1 — DbSetup composition claim (open; doc claims corrected).** `Program.cs` carries `// TODO: Implement ... MigrationInvoker` and never calls `MigrationInvoker.RunMigrationsAsync`; `MigrationInvoker` is implemented but dead code. The claim that DbSetup "applies migrations" was recorded in `6-Docs/catalog.md`, `6-Docs/api/onboarding.md`, `7-Deployment/DbSetup/README.md`, `6-Docs/rules/scripts.md`, and `6-Docs/project-overview.md`; all five were corrected on 2026-10-20 within this delivery (see the [register reconciliation note](#register-reconciliation-note)).
- **DR2 — Hosting model conflict (open; doc claims corrected).** Terraform `main.tf` provisions a Function App (`kind = "functionapp"`, AVM `web-site`); `.github/workflows/release.yml` (`deploy-api`) uses `azure/webapps-deploy` (`v3.0.8`) with startup `dotnet HotshotLogistics.Api.dll`. The hosting model is unresolved at the code/infra level. The prior "Azure Container Apps" claim was removed from all docs on 2026-10-20 (see the [register reconciliation note](#register-reconciliation-note)).
- **DR3 — Workflow vs `hotshot.tfvars` region/resource-group (partially resolved).** Consolidating plan and apply into `.github/workflows/release.yml` eliminated the plan-vs-apply split (`East US 2` / `rg-hotshot-logistics-<env>-eastus2` across both steps), though the committed `7-Deployment/Azure-deploy/environments/dev/hotshot.tfvars` still specifies `East US` / `rg-hotshot-logistics-dev-eastus`.
- **DR4 — Plan/apply overlap (resolved).** `terraform-plan.yml` and `terraform-apply.yml` were deleted and replaced by the single `terraform` job in `.github/workflows/release.yml`, which plans to local `tfplan`, applies it on the same runner, and deletes `tfplan` under `if: always()`. Offline validation runs in `.github/workflows/ci.yml` (`terraform-validate`).
- **DR5 — Missing staging/prod environments (open).** `staging` and `prod` appear in `release.yml` `workflow_dispatch` inputs and the Terraform `environment` variable, but `7-Deployment/Azure-deploy/environments/` contains only `dev/hotshot.tfvars` and all deployments share one backend state key (`hotshotlogistics.tfstate`).
- **DR6 — Action pinning and least-privilege permissions (resolved).** Every third-party action in `.github/workflows/ci.yml` and `.github/workflows/release.yml` is pinned by full 40-character commit SHA. `ci.yml` declares top-level `contents: read` and scopes `security-events: write` strictly to `codeql`; `release.yml` scopes permissions to `id-token: write` and `contents: read`.
- **DR7 — Key Vault name divergence (resolved).** `.github/workflows/release.yml` (`deploy-api`) now reads `sql-connection-string` from `${TARGET_ENV}-hotshot-log-kv-01`, matching Terraform `main.tf`.
- **DR8 — `tfstate` container not provisioned (open).** The `azurerm` backend names blob container `tfstate`; the launchpad solution creates only the resource group and the storage account, so the container must be created out of band before `terraform init` can succeed.
- **DR9 — Serverless knobs on a provisioned tier (open).** `compute_model = "Serverless"` is commented out and the `modules/database` submodule of `avm-res-sql-server` 0.1.5 declares no `compute_model` input; the database is created `GP_S_Gen5_1`, while `min_capacity` and `auto_pause_delay_in_minutes` are set as literals that do not take effect on that tier.
- **DR10 — SQL administrator password (resolved in workflow; secret configuration required).** `release.yml` passes `TF_VAR_administrator_login_password` from `secrets.SQL_ADMIN_PASSWORD` and fails early if unset, though `SQL_ADMIN_PASSWORD` (along with `AZURE_AD_CLIENT_ID` and `AZURE_AD_CLIENT_SECRET`) must be configured in repository secrets before `terraform` runs.
- **DR11 — SQL admin password and Key Vault password diverge (open).** The `sql_server` module receives `var.administrator_login_password`, while the Key Vault `sql-admin-password` secret and the embedded `sql-connection-string` use `random_password.sql_password.result` — two different values — and the connection-string template hardcodes the login name rather than `var.administrator_login`.
- **DR12 — Inert Terraform variables (open).** `min_capacity`, `max_capacity`, `auto_pause_delay_in_minutes`, `azure_ad_client_id`, and `azure_ad_client_secret` are declared (the last two supplied by `release.yml`) but referenced by nothing; the launchpad solution's `location` and `environment` variables are hardcoded away in `main.tf`.
- **DR13 — Branch-name mismatch in the workflow chain (resolved).** Default branch is `main`; `ci.yml` triggers on `pull_request`, `push` to `main`, weekly `schedule`, and `workflow_dispatch`, and `release.yml` chains cleanly via `workflow_run` on successful `CI` runs for pushes to `main` (gated by `release-gate` changed-path checks).
- **DR14 — Secret values written to a generated tfvars file (resolved).** `release.yml` passes all Terraform credentials via `TF_VAR_*` / `ARM_*` environment variables and never writes a generated `hotshot.tfvars` file or uploads a `tfplan` artifact.
- **DR15 — Key Vault secret written unmasked to script/logs (resolved).** `release.yml` (`deploy-api`) masks the Key Vault `sql-connection-string` output with `::add-mask::` and passes it to `dotnet fm migrate` via `env:` (`CONNECTION_STRING`) rather than inline script interpolation.

### Register versioning

These are the entries as originally recorded on 2026-10-19, retained verbatim for audit. DR1 and
DR2 above were corrected in place on 2026-10-20 to name the locations that carried the claims,
so the entries below are the superseded originals.

- **DR1 — DbSetup composition claim.** `6-Docs/dbsetup/architecture.md` states `Program` composes `MigrationInvoker`; `Program.cs` carries `// TODO: Implement ... MigrationInvoker` and never calls `MigrationInvoker.RunMigrationsAsync`. `MigrationInvoker` is implemented but dead code.
- **DR2 — Hosting model conflict.** `6-Docs/technology-stack.md` and `6-Docs/api/architecture.md` say Azure Container Apps; Terraform `main.tf` provisions a Function App (`kind = functionapp`, AVM `web-site`); `API-deploy.yml` uses `azure/webapps-deploy@v4` (App Service) with startup `dotnet HotshotLogistics.Api.dll`. The hosting model is unresolved at the code/infra level.
- **DR3 — Divergent region/resource-group.** `terraform-plan.yml` hardcodes "East US 2" / `rg-hotshot-logistics-dev-eastus2`; `terraform-apply.yml` hardcodes "East US" / `rg-hotshot-logistics-dev-eastus`.
- **DR4 — Plan/apply overlap.** `terraform-plan.yml` embeds a `terraform apply -auto-approve` step gated to `refs/heads/main`, overlapping `terraform-apply.yml`.
- **DR5 — Missing staging/prod environments.** `staging` and `prod` appear in `workflow_dispatch` inputs and the Terraform `environment` variable, but `7-Deployment/Azure-deploy/environments/` contains only `dev/hotshot.tfvars`.
- **DR6 — Action pinning vs standard.** Third-party actions use mutable tags (`azure/login@v2`, `hashicorp/setup-terraform@v3`, `azure/webapps-deploy@v4`, `trufflesecurity/trufflehog@main`); the `github-actions` coding standard requires SHA-pinning and least-privilege `permissions:` (that standard is itself `status: draft`).

### Register reconciliation note

**Dated 2026-10-19; amended 2026-10-20.** This note is not a register entry. It records the
corrections made to the documents behind DR1 and DR2, the amendment made to the register itself
on 2026-10-20, and the locations in which each of the two claims was recorded.

- On 2026-10-19, within this delivery, `6-Docs/technology-stack.md` was corrected. It no
  longer asserts Azure Container Apps; it now states the actual deployment path — an App
  Service plan and a Function App (`kind = "functionapp"`) provisioned by Terraform, deployed
  by `API-deploy.yml` with `azure/webapps-deploy@v4` and the startup command
  `dotnet HotshotLogistics.Api.dll` — and it states that the hosting model is unresolved.
- On 2026-10-19, within this delivery, `6-Docs/dbsetup/architecture.md` was also corrected. Its
  "`Program` composes …" sentence no longer names `MigrationInvoker` — it now states that
  `Program` composes `ArgumentParser`, `EnvironmentManager`, `PasswordManager`, and
  `SqlServerProvisioner` — and its components table now marks `MigrationInvoker` as not wired
  by `Program`. The original DR1 wording, which described the claim before that correction, is
  retained in the [Register versioning](#register-versioning) subsection above.
- The DR1 claim — that DbSetup "applies migrations" — was also carried by
  `7-Deployment/DbSetup/README.md`, whose opening sentence named the target framework of the
  time and went on to claim the tool "creates the SQL Server database and application login,
  then applies FluentMigrator migrations"; by `6-Docs/catalog.md`, whose entry described
  DbSetup as a "Console tool that provisions SQL Server and applies migrations"; by
  `6-Docs/api/onboarding.md`, which said "use
  DbSetup to apply them" for schema changes; by `6-Docs/rules/scripts.md`, which said the
  supported DbSetup CLI "applies FluentMigrator migrations in-process when possible and
  otherwise invokes MigrationRunner"; and by `6-Docs/project-overview.md`, which listed
  DbSetup as a "Console tool that provisions SQL Server and applies FluentMigrator migrations".
  All five files were corrected on 2026-10-20, within this delivery, and the corrected DR1
  above names them. The corrected `6-Docs/dbsetup/architecture.md` and
  `6-Docs/dbsetup/onboarding.md` are the authoritative
  statements of the DbSetup composition and of the tool's migration-inertness; the schema is
  applied by the deploy pipeline with `dotnet fm migrate`, as
  [ci-cd-pipelines.md](ci-cd-pipelines.md) records.
- The Azure Container Apps claim was also carried by `6-Docs/api/architecture.md`,
  `6-Docs/project-overview.md`, `6-Docs/rules/file-organization.md`, the root `README.md`,
  and `1-Presentation/HotshotLogistics.Api/README.md`, in addition to
  `6-Docs/technology-stack.md`, which was corrected on 2026-10-19. All five files were
  corrected on 2026-10-20, within this delivery, and the corrected DR2 above names them. The
  hosting model remains unresolved, and the corrected DR2 does not pick a winner.
- On 2026-10-20 the register was amended in two steps. First, the register was amended to name
  the locations that carried each claim, and DR7–DR15 were added. Then, after the ten
  claim-correction edits landed the same day, DR1 and DR2 were re-worded to state where the
  claims were recorded and that all five locations per claim were corrected. The six original
  entries are retained verbatim in the [Register versioning](#register-versioning) subsection
  above. This register remains the single source of truth for deployment drift for this
  repository.
- On 2026-10-20, after the end-of-run review of this delivery, DR6 was amended in place. Its
  enumeration of mutable action references was completed with the three
  `github/codeql-action/*@v3` references, so it now names every distinct mutable reference in
  the corpus, and it now records that the `github-actions` coding standard's carve-out for
  first-party `actions/*` is conditional on the standard granting it — which it does not — so
  `actions/checkout@v5` and `actions/setup-dotnet@v4` are not exempted from the SHA-pinning
  requirement. The 2026-10-19 wording of DR6 is retained verbatim in the
  [Register versioning](#register-versioning) subsection above.

## Related documentation

- [Documentation index](../README.md)
- [Deployment and build documentation](README.md)
- [CI/CD pipelines](ci-cd-pipelines.md)
- [Terraform modules and infrastructure](terraform-modules.md)
- [Documentation standard](../documentation-standard.md)
- [Documentation ontology](../documentation-ontology.md)
- [Component and owner catalog](../catalog.md)
- [Technology stack](../technology-stack.md)
- [API architecture](../api/architecture.md) and [API onboarding](../api/onboarding.md)
- [DbSetup architecture](../dbsetup/architecture.md) and [DbSetup onboarding](../dbsetup/onboarding.md)
- [Admin dashboard architecture](../admin-dashboard/architecture.md)
- [github-actions coding standard](../standards/github-actions/github-actions.standards.md)
- [Security guidelines](../security.md)
- Source roots: [7-Deployment/README.md](../../7-Deployment/README.md) and [7-Deployment/DbSetup/README.md](../../7-Deployment/DbSetup/README.md)
