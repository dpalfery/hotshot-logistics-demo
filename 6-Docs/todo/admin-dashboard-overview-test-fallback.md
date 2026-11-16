---
id: todos/admin-dashboard-overview-test-fallback
title: Remove DashboardOverview test-mode fallback data and debug logging
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Remove DashboardOverview test-mode fallback data and debug logging

## Finding

`1-Presentation/admin-dashboard/src/components/dashboard/DashboardOverview.tsx` (`DashboardOverview`)
ships hard-coded `testJobs`, `testDrivers` and `testInvoices` and substitutes them when
`window.__BYPASS_AUTH__` is set and a query returned nothing. The same component logs a
`=== DASHBOARD DEBUG INFO ===` block with `console.log` on every render, and query errors reach only
`console.error`.

## Why it matters

Mock data in production code can display invented figures if the bypass flag is ever set outside a
test, and it hides API failures behind plausible numbers. The React standard (Hygiene, Safety)
forbids `console.log` in merged code and requires network errors to surface to the user. The
Playwright specs already mock every API route, so the fallback is not needed by the tests.

## Next action

Delete the fallback objects and debug logging, surface query errors in the overview, and confirm
`tests/dashboard-overview.spec.ts` still passes with its own route mocks. Found while planning
`plans/2026-11-16-dashboard-job-status-cards` (decision D10).
