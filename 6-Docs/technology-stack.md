---
id: technology-stack
title: Technology stack
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-10-20
---

# Technology Stack

## Backend (.NET)

- **Framework**: .NET 8 ASP.NET Core Web API (Azure App Service / Function App — see [Hosting model](#hosting-model))
- **Database**: SQL Server with native ADO.NET (Entity Framework is prohibited)
- **Testing**: xUnit, FluentAssertions
- **Configuration**: Azure App Configuration, Key Vault; local API uses .NET user secrets
- **Build Tools**: dotnet CLI
- **Migrations**: FluentMigrator (required for all schema changes)

## Frontend (Admin Dashboard)

- **Framework**: Next.js 15, React 19
- **Styling**: Tailwind CSS
- **Charts**: Recharts
- **Tables**: TanStack Table
- **State**: TanStack Query
- **Validation**: Zod
- **UI**: Headless UI, Heroicons, Radix
- **Auth**: MSAL
- **Realtime**: SignalR client
- **Tests**: Playwright

## Infrastructure

- **Cloud**: Azure (App Service / Function App, SQL Server, App Configuration, Key Vault)
- **Containerization**: Docker Compose under `7-Deployment/`
- **CI/CD**: GitHub Actions
- **Deployment**: Terraform (`7-Deployment/Azure-deploy/`)

## Hosting model

The API's hosting model is **unresolved**, and this document does not pick a winner.

- **Actual deployment path**: Azure App Service / Function App. Terraform under
  `7-Deployment/Azure-deploy/` provisions an App Service plan, a Function App
  (`kind = "functionapp"`), and related resources; `API-deploy.yml` deploys with
  `azure/webapps-deploy@v4` and the startup command `dotnet HotshotLogistics.Api.dll`.
- **Why it is unresolved**: a Terraform-provisioned Function App and an App Service
  deploy action are not the same thing, so the code and the infrastructure do not agree
  on the hosting target.
- **No container target**: no Container Apps resource is declared in any Terraform file,
  and no workflow builds a container image.
- **Known contradiction**: an earlier revision of this file asserted Azure Container Apps
  as the hosting model. The code and the infrastructure contradict that claim, and it
  does not describe the deployment path this repository implements. It is recorded as
  **DR2** in the canonical drift register, and the ambiguity itself is deliberately left
  unresolved here.

## Deployment drift

The [canonical drift register](devops/deployment-architecture.md#canonical-drift-register),
held in [the deployment architecture document](devops/deployment-architecture.md), is the
single source of truth for deployment drift. Documents reference drift items by their DR
id rather than restating them; this document points at the register the same way and does
not duplicate it.

## Development Tools

- **Version Control**: Git
- **Code Quality**: StyleCop, EditorConfig
- **Package Management**: NuGet (backend), npm (dashboard)

## Key constraints

- Native ADO.NET for all data access
- No Entity Framework
- No Pulumi, Bicep, or Ansible
- No driver mobile app in this tree

## Current Status

- Active development
- Cataloged runnables are documentation status `current`
- ADO.NET migration from Entity Framework is complete
