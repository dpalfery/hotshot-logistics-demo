---
id: todos/job-repository-explicit-columns
title: Name columns in JobRepository read queries
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Name columns in JobRepository read queries

`4-Persistence/HotshotLogistics.Data/Repositories/JobRepository.cs` still reads
jobs with `SELECT * FROM {GetTableName()}` in `GetJobsAsync` and in the
status, driver and customer lookups (about lines 97, 145, 168, 190 and 212).
This predates the job status cards change, which added only an explicit
`GROUP BY Status` query.

The data-access standard (`6-Docs/standards/data-access-layer/README.md`, "List
columns explicitly") and the SQL standard (`6-Docs/standards/sql/README.md`,
"Name the columns") forbid `SELECT *`: it couples the mapper to column order and
silently picks up any column a later migration adds.

## Next action

Replace each `SELECT *` with the column list the job mapper reads, keep the
existing filters, ordering and paging, and cover the changed queries with the
existing `JobRepositoryTests` against the test database. Check other
repositories for the same pattern in the same change.
