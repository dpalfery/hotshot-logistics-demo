---
id: todos/health-checks
title: Add API health checks
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Add API health checks

The [C# coding standard](../standards/csharp/README.md#http-pipeline) calls for `/health` checks for database, queue, and API dependencies, plus readiness/liveness integration. The API currently has no `AddHealthChecks()` or `MapHealthChecks()` registration.

**Next action:** Define the required health-check endpoints and dependency checks, implement them, and add focused tests.
