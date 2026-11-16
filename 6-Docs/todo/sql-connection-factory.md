---
id: todos/sql-connection-factory
title: Introduce a SQL connection factory
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Introduce a SQL connection factory

The data-access standard calls for an `ISqlConnectionFactory`, but the
repository does not currently define one. Several ADO.NET repositories create
`SqlConnection` instances directly.

`JobRepository.GetJobStatusCountsAsync`, added by the
job status dashboard plan (`2026-11-16-job-status-dashboard.md`) under its
decision D5, follows the same `new SqlConnection(ConnectionString)` pattern as its
sibling methods and must be part of the migration.

## Next action

Plan and implement the factory and its dependency-injection registration.
Migrate repository connection creation without changing repository behavior,
and cover connection ownership, disposal, cancellation, and failures with
focused tests. Review migration and provisioning boundaries separately.
