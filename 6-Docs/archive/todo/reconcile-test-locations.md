---
id: todos/reconcile-test-locations
title: Reconcile test locations with file organization rule
doc-type: todo
status: superseded
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-14
---

# Reconcile test locations with file organization rule

Closed on 2026-11-14 by documenting scoped exceptions for `1-Presentation/admin-dashboard/tests/` and `7-Deployment/DbSetup/HotshotLogistics.DbSetup.Tests/` in [File Organization Rule](../../rules/file-organization.md).

The file organization rule (`6-Docs/rules/file-organization.md`) specifies that all testing infrastructure and test files belong under `5-Test/`. Currently, two test suites live outside `5-Test/`:

- Playwright end-to-end tests and helpers in `1-Presentation/admin-dashboard/tests/`
- DbSetup test project and source in `7-Deployment/DbSetup/`

## Next action

Investigate test execution, project dependencies, and directory configuration for both suites. Propose either migrating them under `5-Test/` or documenting a scoped exception in the file organization rule. Do not move files until the approach is approved.
