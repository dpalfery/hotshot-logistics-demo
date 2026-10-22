---
id: dbsetup/onboarding
title: DbSetup onboarding
doc-type: onboarding
status: current
component: DbSetup
source-root: 7-Deployment/DbSetup
owner: unassigned
last-reviewed: 2026-10-19
---

# DbSetup onboarding

## Dependencies

- .NET 8 SDK
- SQL Server with permission to create databases and logins
- A privileged connection string, supplied through the **process environment or a secret store**, never from a file in the repository

## Setup

From the repository root:

```bash
dotnet run --project 7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj -- --server "<sql-server-instance>" --db-name "hotshot_logistics"
```

The tool does not prompt for missing values. A missing `--server` with no `HOTSHOT_DB_SERVER` set fails validation and exits with code 1. For CI, pass `--non-interactive` together with `--sa-connection-string` from the **process environment or a secret store**, never from a file in the repository; `CI_SA_CONNECTION_STRING` supplies the same value when the flag is absent.

### Environment overrides

`ArgumentParser.ApplyEnvironmentOverrides` fills values that were not supplied on the command line, or that are still at their defaults:

| Variable | Fills |
| --- | --- |
| `HOTSHOT_DB_SERVER` | `--server` |
| `HOTSHOT_DB_NAME` | `--db-name`, only when it is still the default `hotshot_logistics` |
| `HOTSHOT_DB_APP_USER` | `--app-user`, only when it is still the default `hotshot_app` |
| `HOTSHOT_DB_PASSWORD` | `--password` |
| `CI_SA_CONNECTION_STRING` | `--sa-connection-string` |

Do not persist generated passwords into tracked files. `--persist-env` asks for consent and writes the generated connection string to the OS environment under the name `DB_CONNECTION_STRING` — user scope on Windows, an export line appended to the shell profile on macOS and Linux — never to the repository.

## Debug path

- Logs go to the console through `Microsoft.Extensions.Logging` with the console provider. `Program` does not use `SecureLogger`, so secret masking is not applied to console output.
- The tool prints the generated application connection string to the console, including the generated password, only when neither `--persist-env` nor `--non-interactive` is supplied. With `--non-interactive` it prints nothing: the connection string is neither echoed nor persisted to the OS environment. A consented `--persist-env` that then fails to set the variable prints the connection string again as manual setup instructions. Treat console output as a secret.
- Failures roll back artifacts created in that run (`SqlServerProvisioner`).
- The CLI does not run migrations. `MigrationInvoker` is implemented but `Program` never invokes it — see [DbSetup architecture](architecture.md).

## Command line options

| Option | Purpose |
| --- | --- |
| `--server` | SQL Server instance |
| `--sa-connection-string` | Privileged connection string from a secret store or process environment |
| `--db-name` | Target database (default `hotshot_logistics`) |
| `--app-user` | Application login name (default `hotshot_app`) |
| `--password` | Application password (omit to generate) |
| `--non-interactive` | No prompts; requires a privileged connection string |
| `--force` | Allow destructive operations. Accepted and logged, but not consulted by the current provisioning path |
| `--persist-env` | Persist the generated connection string to the OS environment with consent, never to the repo |
| `--help`, `-h`, `-?` | Print usage and exit 0 |

Only the long forms exist. There are no `-s` or `--sa-cs` aliases.

See the source-root [DbSetup README](../../7-Deployment/DbSetup/README.md) for the overview only.

## CI and deployment

The GitHub Actions workflows do **not** invoke the DbSetup CLI. The pipelines apply the schema themselves: `.github/workflows/API-deploy.yml` installs the FluentMigrator CLI and runs `dotnet fm migrate` directly against the `HotshotLogistics.Data` assembly, so the DbSetup migration path is not on the CI/CD path at all. `.github/workflows/dotnet-buid-test.yml` compiles the DbSetup project as part of the whole-repository build, but no workflow executes the tool.

DbSetup is a provisioning tool, not a runtime service, and it is not part of the CI/CD path. It runs when someone provisions a database by hand. For how the deployed stack and its pipelines are wired, see [deployment architecture](../devops/deployment-architecture.md) and the [DevOps documentation index](../devops/README.md).

## Non-standard operating procedures

- Entity Framework migrations are forbidden. The intended path is FluentMigrator migrations only, and the CLI does not apply them today because `MigrationInvoker` is not invoked — see [DbSetup architecture](architecture.md).
- Do not copy connection strings with real passwords into docs, scripts, or READMEs. Use placeholders such as `<sa-connection-string>`.
