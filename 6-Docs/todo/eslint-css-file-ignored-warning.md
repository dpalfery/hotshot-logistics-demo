---
id: todos/eslint-css-file-ignored-warning
title: Stop ESLint warning on CSS files in changed-file lint runs
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Stop ESLint warning on CSS files in changed-file lint runs

`1-Presentation/admin-dashboard/eslint.config.mjs` configures only JavaScript
and TypeScript, and `package.json` runs `"lint": "eslint"`. When a lint run is
given a changed `src/app/globals.css`, ESLint prints a "File ignored because no
matching configuration was supplied" warning. The warning predates the
job-status dashboard change and appears in the baseline.

It matters because the warning is noise in task-gate and review output, and
CSS changes (for example the `@theme` colour tokens in `globals.css`) get no
lint check at all.

## Next action

Either filter changed-file lint invocations to `*.{js,jsx,ts,tsx,mjs}`, or add
CSS linting (ESLint's CSS language plugin or Stylelint) so `globals.css` is
actually checked. Confirm `npm run lint` and the task gate run clean.
