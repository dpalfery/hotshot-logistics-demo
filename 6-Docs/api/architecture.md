---
id: api/architecture
title: API architecture
doc-type: architecture
status: current
component: Api
source-root: 1-Presentation/HotshotLogistics.Api
owner: unassigned
last-reviewed: 2026-10-20
code-refs:
  - JobController
  - TestAuthHandler
  - ExceptionHandlingMiddleware
---

# API architecture

## Overview

`HotshotLogistics.Api` is an ASP.NET Core Web API. Its hosting model is unresolved — see the canonical drift register (DR2) in [deployment architecture](../devops/deployment-architecture.md); the actual deployment path is an Azure App Service plan / Function App provisioned by Terraform, deployed by `API-deploy.yml`. It is the HTTP and SignalR edge for jobs, drivers, customers, billing, tracking, and assignments. It depends on Application, Domain, and Persistence; it does not own business invariants.

## Architecture

```mermaid
flowchart TD
  client[Admin dashboard]
  api[HotshotLogistics.Api]
  app[Application services]
  data[ADO.NET repositories]
  db[(SQL Server)]
  client --> api
  api --> app
  app --> data
  data --> db
```

`Program` wires user secrets, optional Azure App Configuration, repository and application DI, authentication, CORS, controllers, and `RealtimeHub` at `/realtime`.

## Components and interfaces

| Surface | Role |
| --- | --- |
| `JobController` and sibling controllers under `Controllers/` | HTTP API, route prefix `api/[controller]` |
| `TestAuthHandler` | Development and test authentication |
| JWT Bearer + Microsoft Identity Web | Non-development Azure AD B2C |
| `ExceptionHandlingMiddleware` | Structured error responses (ProblemDetails-style payloads) |
| Application `RealtimeHub` | Real-time updates mapped from the API host |

Authorization policies include `Admin`, `Manager`, `Driver`, `Customer`, and combined roles.

## Data models

Request and response DTOs live in Domain. Persistence uses native ADO.NET. See [data access](../data-access.md).

## Error handling

Controllers and `ExceptionHandlingMiddleware` map validation and domain exceptions to HTTP status codes. Do not return secrets or stack traces to clients in production.

## Testing strategy

API tests live under `5-Test/` (`HotshotLogistics.Api.Tests`, integration tests). The `Program` type is public (`Program.Api.cs`) so the test host can use the real composition root.
