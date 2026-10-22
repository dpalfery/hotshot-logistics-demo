# Hotshot Logistics

Cloud-native platform for hotshot delivery operations: job assignment, driver and customer management, tracking, and billing. It replaces paper-based dispatch with a web admin surface and an ASP.NET Core API on Azure.

This repository is in **active development**. Cataloged runnables are documentation status `current`. There is no driver mobile app in the tree; do not treat one as current.

## Maintained runnables

| Runnable | What it is | Source README |
| --- | --- | --- |
| Api | ASP.NET Core Web API (.NET 8), hosting model unresolved (App Service / Function App — see DR2). Native ADO.NET and FluentMigrator; Entity Framework is prohibited. | [HotshotLogistics.Api](1-Presentation/HotshotLogistics.Api/README.md) |
| admin-dashboard | Next.js app for jobs, drivers, customers, tracking, and billing. | [admin-dashboard](1-Presentation/admin-dashboard/README.md) |
| DbSetup | .NET CLI that provisions SQL Server databases and application accounts for local and CI use. | [DbSetup](7-Deployment/DbSetup/README.md) |

The [component catalog](6-Docs/catalog.md) is the authoritative inventory. Shared libraries under `0-Base`, `2-Application`, `3-Domain`, and `4-Persistence` are not cataloged products.

## Onboarding

1. Install the **.NET 8 SDK**, **Node.js** (for the dashboard), and a **SQL Server** instance (local, Docker, or remote).
2. Restore and build the solution: `dotnet restore HotshotLogistics.sln` then `dotnet build`.
3. Provision the database — [DbSetup onboarding](6-Docs/dbsetup/onboarding.md).
4. Configure and run the API — [API onboarding](6-Docs/api/onboarding.md). Launch URLs are `https://localhost:5001` and `http://localhost:5000`.
5. Run the dashboard — [admin-dashboard onboarding](6-Docs/admin-dashboard/onboarding.md).

Deployment assets live under [7-Deployment](7-Deployment/README.md). Infrastructure is Terraform, not Pulumi. Do not put secrets, connection strings, or `.env` files in this repository. See [SECURITY.md](SECURITY.md) and [6-Docs/security.md](6-Docs/security.md).

## Documentation

The [documentation index](6-Docs/README.md) is the entry point. Agents should follow [AGENTS.md](AGENTS.md).

## Community

- [Security policy](SECURITY.md)
