---
id: todos/health-checks
title: Add API health checks
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Add API health checks

The ASP.NET Core practices rule requires `/health` checks for database, queue, and API dependencies, plus readiness/liveness integration. The API currently has no `AddHealthChecks()` or `MapHealthChecks()` registration.

**Next action:** Define the required health-check endpoints and dependency checks, implement them, and add focused tests.
