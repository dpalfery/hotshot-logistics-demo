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

## Next action

Plan and implement the factory and its dependency-injection registration.
Migrate repository connection creation without changing repository behavior,
and cover connection ownership, disposal, cancellation, and failures with
focused tests. Review migration and provisioning boundaries separately.

## Additional occurrences

- `4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs`: the job status summary
  query added by `plans/2026-11-16-dashboard-job-status-cards` (task T1) opens its connection with
  `new SqlConnection(ConnectionString)` from `BaseRepository<Job>`. This departs from the DAL
  standard's `ISqlConnectionFactory` rule under that plan's approved decision D8, which deferred the
  factory to this todo. Include the summary query when migrating `JobRepository`.
