---
id: todos/sql-connection-factory
title: Introduce a SQL connection factory
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Introduce a SQL connection factory

The data-access standard calls for an `ISqlConnectionFactory`, but the
repository does not currently define one. Several ADO.NET repositories create
`SqlConnection` instances directly.

The data-access standard forbids `new SqlConnection` at a call site. Known
call sites include every method of
`4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs`, among them
`GetJobStatusSummaryAsync`, which the job-status dashboard plan added with
`new SqlConnection(ConnectionString)` under its decision D7 (a sanctioned
exception while the factory is absent). Each new repository method written this
way enlarges the migration, and connection creation cannot be substituted or
instrumented in one place until the factory exists.

## Next action

Plan and implement the factory and its dependency-injection registration.
Migrate repository connection creation without changing repository behavior,
and cover connection ownership, disposal, cancellation, and failures with
focused tests. Review migration and provisioning boundaries separately.
