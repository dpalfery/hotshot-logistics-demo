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

This breaks the data-access standard (`6-Docs/standards/data-access-layer/README.md`):
call sites cannot be tested without a real connection and connection policy is
repeated per method. The `Admin dashboard job status cards` plan (decision D7)
added one more call site under an approved departure:
`JobRepository.GetJobStatusCountsAsync` in
`4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs` opens
`new SqlConnection(ConnectionString)` like its sibling `GetJobCountAsync`.

## Next action

Plan and implement the factory and its dependency-injection registration.
Migrate repository connection creation without changing repository behavior,
and cover connection ownership, disposal, cancellation, and failures with
focused tests. Review migration and provisioning boundaries separately.
