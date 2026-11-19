---
id: todos/admin-dashboard-playwright-baseline
title: Admin dashboard Playwright tests marked fixme
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Admin dashboard Playwright tests marked fixme

Eleven dashboard Playwright tests were `test.fixme` because the app did not implement the behaviour they check. Ten now pass. The eleventh moved to, and was later resolved in, [Resolve unresolved DashboardOverview issues](admin-dashboard-job-status-unresolved.md) because it depended on the SignalR event contract.

## What changed

- The overview shows a loading, error or data state per statistic card and for Recent Jobs, and Recent Jobs lists the jobs from the API (`No recent jobs` when empty). The test-mode fallback data in `DashboardOverview` is gone.
- `getJobs`, `getDrivers` and `getInvoices` validate their response shape; a response that fails is an error state.
- The Quick Actions are links to `/jobs?action=create`, `/drivers?action=add` and `/billing?action=generate`. `/jobs` opens its create form from `?action=create`. The Drivers and Billing forms are tracked in [Build the Add Driver and Generate Invoice forms](../../todo/admin-dashboard-quick-action-forms.md).
- The five Tracking tests inject SignalR messages through a network-level mock (`tests/utils/signalr-mock.ts`) instead of the never-read `window.mockSignalRService`. `signalr.ts` no longer loses its connection when React StrictMode mounts, unmounts and mounts the dashboard again.
- Two tests mocked routes the app does not call (`/api/dashboard/stats`, `/api/jobs*`); they now mock `/api/job`.

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
