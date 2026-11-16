---
id: todos/admin-dashboard-console-logging
title: Remove console logging from admin dashboard services
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Remove console logging from admin dashboard services

Pre-existing `console` calls remain in admin dashboard modules the job status
cards change did not touch:

- `1-Presentation/admin-dashboard/src/services/signalr.ts`: `console.log('SignalR connected')` (about line 33) and `console.error` on connection failure (about line 35).
- `1-Presentation/admin-dashboard/src/config/auth.ts`: the MSAL `loggerCallback` writes every event with `console.log` (about lines 55 and 59), plus `console.warn` and `console.error` around lines 98 and 135.

The React standard (`6-Docs/standards/react/README.md`) allows no `console.log`
in merged code and notes that an error which only reaches the console is
invisible in production. The MSAL callback also logs every info-level event in
development.

## Next action

Route these through the dashboard's error and telemetry handling (or surface
connection and auth failures in the UI), drop the success `console.log`, and
keep MSAL logging at warning level without console output in production.
