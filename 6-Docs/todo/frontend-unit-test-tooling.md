---
id: todos/frontend-unit-test-tooling
title: Add frontend unit-test tooling
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-14
---

# Add frontend unit-test tooling

The dashboard currently declares Playwright for E2E tests and has no unit or
component test runner (see the [test coding standard](../standards/test/README.md)).
Adding Vitest and React Testing Library is proposed to cover frontend unit and
component tests.

## Next action

Plan and add the agreed unit-test tooling, scripts, and focused representative
tests. Review dependency and configuration changes before implementation, and
keep Playwright for end-to-end coverage.
