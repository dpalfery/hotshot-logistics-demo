---
id: todos/migrationrunner-msb3539-warning
title: Fix MigrationRunner MSB3539 build warning
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Fix MigrationRunner MSB3539 build warning

## Finding

Building `4-Persistence/MigrationRunner/MigrationRunner.csproj` reports MSBuild warning MSB3539
(`BaseIntermediateOutputPath` modified after use). The project sets `BaseIntermediateOutputPath`,
`IntermediateOutputPath` and `OutputPath` in its own property group (lines 27-29), after the SDK has
already read `BaseIntermediateOutputPath`. The warning predates
`plans/2026-11-16-dashboard-job-status-cards` and was reported by its workers.

## Why it matters

MSBuild ignores the late `BaseIntermediateOutputPath`, so the project's intermediate files may not
land where the property says, and per-task `--artifacts-path` isolation used by the task gates can
be undermined. The standing warning also hides new warnings in build output.

## Next action

Remove the hard-coded output path properties (or move `BaseIntermediateOutputPath` to a
`Directory.Build.props` that is imported before the SDK), confirm the build is warning-free, and
confirm the MigrationRunner still applies migrations through `dev-env-up.sh`.
