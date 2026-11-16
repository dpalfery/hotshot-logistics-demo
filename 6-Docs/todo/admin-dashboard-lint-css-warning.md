---
id: todos/admin-dashboard-lint-css-warning
title: Stop linting CSS files with ESLint
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Stop linting CSS files with ESLint

When the dashboard lint gate passes `1-Presentation/admin-dashboard/src/app/globals.css`
to ESLint, ESLint reports a "file ignored" warning at line 0: `eslint.config.mjs`
configures only JavaScript and TypeScript (`eslint-config-next`), so no config
matches CSS. The baseline on the base branch shows the same warning, so it is
pre-existing, but it is noise in every review and can hide a real warning.

## Next action

Limit the files the lint gate hands to ESLint to `.js`, `.jsx`, `.ts`, `.tsx`
and `.mjs`, or add a CSS linter (for example Stylelint) if CSS rules are wanted.
Confirm the gate then reports no warning for a change that touches `globals.css`.
