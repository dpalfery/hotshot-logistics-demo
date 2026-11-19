---
id: todos/admin-dashboard-quick-action-forms
title: Build the Add Driver and Generate Invoice forms
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-19
---

# Build the Add Driver and Generate Invoice forms

The overview's Quick Actions link to `/drivers?action=add` and `/billing?action=generate`. `/jobs?action=create` opens the existing create-job form, but the Drivers and Billing pages have no add or generate form, so those two links land on the plain page.

## Next action

Add a driver form (`apiService.createDriver` exists) opened by `?action=add`, and an invoice generation form (`apiService.createInvoice` exists) opened by `?action=generate`. Read the parameter the way `JobsManagement` does: seed the initial state from `useSearchParams`, and wrap the page in `Suspense`. Add a Playwright test for each that checks the form opens.
