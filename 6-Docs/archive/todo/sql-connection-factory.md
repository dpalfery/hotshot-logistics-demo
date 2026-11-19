---
id: todos/sql-connection-factory
title: Introduce a SQL connection factory
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
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

Resolved on 2026-11-19: `ISqlConnectionFactory` (Contracts) and the internal `SqlConnectionFactory` (Data, registered Singleton in `AddHotshotRepositories()`) replace every repository `new SqlConnection`, including the shared `BaseRepository<T>`, which moved from Core to Data. `MigrationRunner` and DbSetup provisioning keep direct connections by design (separate review).
