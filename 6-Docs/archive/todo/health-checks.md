---
id: todos/health-checks
title: Add API health checks
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Add API health checks

The [C# coding standard](../../standards/csharp/csharp.standards.md#http-pipeline) calls for `/health` checks for database, queue, and API dependencies, plus readiness/liveness integration. The API currently has no `AddHealthChecks()` or `MapHealthChecks()` registration.

## Resolution

Resolved 2026-11-19. `AddApiHealthChecks()` / `MapApiHealthChecks()` (`Health/HealthCheckExtensions.cs`) expose three
anonymous endpoints that report check names and statuses only (no descriptions, data or exceptions):

- `/health/live`: runs no checks; 200 while the process serves requests.
- `/health/ready`: runs the checks tagged `ready`; 503 when the database is unreachable.
- `/health`: runs every check.

`HotshotLogistics.Data.Health.DatabaseHealthCheck` opens a connection and runs `SELECT 1` under
`Health:Database:TimeoutSeconds` (default 3). It opens the connection through `ISqlConnectionFactory`, so a missing connection string makes the check
unhealthy instead of failing startup. The database is the only dependency: the API has no queue and no
downstream API it must reach before serving.

Tests: `HotshotLogistics.Api.Tests.HealthCheckTests` (database down or unset),
`HotshotLogistics.Tests.Health.DatabaseHealthCheckTests`, `HotshotLogistics.IntegrationTests.HealthCheckIntegrationTests`
(database up).
