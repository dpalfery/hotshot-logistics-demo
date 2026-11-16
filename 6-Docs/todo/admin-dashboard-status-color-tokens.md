---
id: todos/admin-dashboard-status-color-tokens
title: Replace inline job status badge colours with theme tokens
doc-type: todo
status: current
component: admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
---

# Replace inline job status badge colours with theme tokens

The job status cards colour their icon badges with raw inline styles:
`1-Presentation/admin-dashboard/src/components/dashboard/JobStatusCard.tsx` applies
`style={{ backgroundColor: color }}`, and the hex values (`#3B82F6`, `#0EA5E9`,
`#0284C7`, `#0369A1`) are hardcoded in
`1-Presentation/admin-dashboard/src/components/dashboard/jobStatusCardConfig.ts`.

The [React standard](../standards/react/README.md#styling) says to use Tailwind
utility classes rather than raw inline styles and to define reusable theme values as
CSS custom properties and Tailwind theme tokens. Plan decision D10 of the
job status dashboard plan (`2026-11-16-job-status-dashboard.md`) approved the
inline style as a task-scoped exception, because Tailwind 4 palette classes are OKLCH
and do not render these exact hex values, and dynamic `bg-[${color}]` classes are not
generated. Left as is, the colours are invisible to the theme and every new status
card repeats the exception.

## Next action

Declare the four status colours as Tailwind 4 `@theme` tokens (for example
`--color-status-pending`) in the dashboard's stylesheet, map each status to a static
utility class (`bg-status-pending`) in `jobStatusCardConfig.ts`, and remove the inline
`style` from `JobStatusCard.tsx`. Keep the rendered colours identical and cover them
with the existing `tests/job-status-cards.spec.ts` Playwright test.
