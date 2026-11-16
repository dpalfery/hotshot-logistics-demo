---
id: todos/playwright-live-test-standard
title: Reconcile the test standard with the opt-in live Playwright test
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Reconcile the test standard with the opt-in live Playwright test

The test standard (`6-Docs/standards/test/README.md`, section "Frontend
(Playwright)") says every API call is mocked and Playwright must never hit a
real API. The job-status dashboard plan (task T2, decision D14) added an `@live`
test to `1-Presentation/admin-dashboard/tests/job-status-cards.spec.ts` that is
skipped unless `LIVE_API=1` and then calls the local API on the dev test
database. `6-Docs/admin-dashboard/architecture.md` documents how to run it, but
the standard still states the absolute rule.

This matters because the standard and the code now disagree: a reviewer or
worker following the standard would flag or remove the test, and the next
end-to-end check that needs a live chain has no sanctioned pattern to follow.

## Next action

Decide whether opt-in live tests are allowed. If they are, amend the test
standard with the conditions (opt-in flag, test database only, no committed
secrets, excluded from default runs and CI gates) and cite the `@live` test as
the example. If they are not, replace the `@live` test with the API
integration tests that already prove the database-to-endpoint link and remove
the live run from the architecture doc.
