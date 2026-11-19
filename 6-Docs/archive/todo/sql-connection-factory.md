---
id: todos/sql-connection-factory
title: Introduce a SQL connection factory
doc-type: todo
status: superseded
documentation-impact: no-design-change
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

## Decisions

| ID | Question | Answer | Status |
|---|---|---|---|
| D1 | Where does the durable record of this completed todo live? | The resolution in this archived todo is the record. | Approved |

## Closeout mapping

| ID | Destination |
|---|---|
| D1 | Documentation waiver — the resolution in this archived todo is the lasting record |

## Design harvest

Documentation waiver — nothing survives because the completed work is recorded in this archived todo and canonical docs were updated when the work landed.
