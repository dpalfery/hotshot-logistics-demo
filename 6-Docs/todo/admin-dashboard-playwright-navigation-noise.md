---
id: todos/admin-dashboard-playwright-navigation-noise
title: Stop unmocked billing and SignalR calls in the dashboard navigation test
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Stop unmocked billing and SignalR calls in the dashboard navigation test

The Playwright test `Navigation › should navigate to different sections from sidebar` in `1-Presentation/admin-dashboard/tests/dashboard-overview.spec.ts` passes, but its log shows two errors that were there before the `2026-11-16-admin-dashboard-job-status-cards` run:

- **Billing fixture error.** The test opens `/billing` without the invoice mocks that `billing-management.spec.ts` and `tests/utils/test-helpers.ts` set up for `**/api/billing/invoices**`, so the Billing page's request fails.
- **SignalR connection failure.** The test opens `/tracking` (and the dashboard), which start the real SignalR client with no hub to connect to, so the page logs `Failed to connect to SignalR`.

This matters because noise in a passing test hides real errors. A developer reading a red log during test-first work cannot tell these errors from the ones their own change caused, and a future check that fails on console errors would fail here for no reason.

## Next action

Give the navigation test the same API fixtures as the page specs, for example a shared helper in `tests/utils/test-helpers.ts` that mocks the billing endpoints. Stub or abort the SignalR negotiate request, as the `should handle SignalR connection errors gracefully` test in `tracking-dashboard.spec.ts` does, or assert only on navigation and filter the expected error out. Coordinate the SignalR stub with the test-seam decision in [Admin dashboard Playwright tests marked fixme](admin-dashboard-playwright-baseline.md).
