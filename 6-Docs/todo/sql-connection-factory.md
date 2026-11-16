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
`SqlConnection` instances directly: every `BaseRepository<T>` method in
`0-Base/HotshotLogistics.Core/Repositories/BaseRepository.cs`, and the custom
queries in the `4-Persistence/HotshotLogistics.Data/Repositories/` classes. That
includes `JobRepository.GetJobStatusCountsAsync`, which was added by plan
`2026-11-16-admin-dashboard-job-status-cards` (decision D12) and follows the
existing pattern until the factory exists.

## Next action

Plan and implement the factory and its dependency-injection registration.
Migrate repository connection creation without changing repository behavior,
and cover connection ownership, disposal, cancellation, and failures with
focused tests. Review migration and provisioning boundaries separately.
