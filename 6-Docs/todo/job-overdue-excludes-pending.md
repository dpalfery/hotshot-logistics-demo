---
id: todos/job-overdue-excludes-pending
title: Check why overdue jobs exclude Pending jobs
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Check why overdue jobs exclude Pending jobs

`JobRepository.GetOverdueJobsAsync` skips jobs whose status is `Received` or `Pending`: it passes `JobStatus.Pending` as
the parameter named `@CancelledStatus`. The server has no cancelled status (`Pending`, `Assigned`, `EnRoute`,
`Received`), so a pending job past its estimated delivery time is never reported overdue, while the `isOverdue` filter in
`GetJobsAsync` excludes only `Received`. The two overdue definitions disagree.

## Next action

The owner decides whether a pending job can be overdue. If it can, drop the second status from the query and test both
definitions agree; if not, name the parameter for what it does and align the `isOverdue` filter.
