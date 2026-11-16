---
id: todos/job-status-summary-global-test-isolation
title: Isolate the global job status summary delta test from concurrent writers
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Isolate the global job status summary delta test from concurrent writers

## Finding

`5-Test/tests/HotshotLogistics.IntegrationTests/JobControllerIntegrationTests.cs`
(`GetJobStatusSummary_WithoutCustomerId_IncludesJobsFromBothCustomers`) reads the unscoped
`GET /api/job/status-summary`, inserts five jobs, reads it again and asserts exact deltas. The
unscoped count covers every job in the shared `hotshot_logistics_test` database, so any job
created, moved or deleted between the two reads by another writer changes the delta.
`[Collection(DatabaseCollection.Name)]` serialises classes only inside one test project; the
`Tests` project and other worktrees use the same database at the same time.

## Why it matters

The test can fail intermittently when other database suites or worktrees run in parallel, which
erodes trust in the gate. The test standard ("Parallel runs must not share") says the shared
database is protected only by unique ids and collections; an exact global delta relies on neither.

## Next action

Make the global assertion robust to other writers: for example assert `after - before >=` the
seeded counts, or verify the seeded jobs are included through a scoped query and keep the global
read as a non-negative/shape check. Found during `plans/2026-11-16-dashboard-job-status-cards`
(task T1); the plan's T3 risk notes the same race for the live check.
