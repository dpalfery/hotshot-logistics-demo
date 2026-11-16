---
id: todos/migrationrunner-msb3539
title: Fix MigrationRunner MSB3539 build warning
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Fix MigrationRunner MSB3539 build warning

`4-Persistence/MigrationRunner/MigrationRunner.csproj` sets
`BaseIntermediateOutputPath` (and `IntermediateOutputPath`, `OutputPath`) in its
project body (about lines 27 to 29). MSBuild has already used
`BaseIntermediateOutputPath` by then, so every build emits MSB3539 ("The value of
the property BaseIntermediateOutputPath was modified after it was used by
MSBuild"). The paths are also Windows-style (`obj\`, `bin\`). The warning is
pre-existing and appears on every solution build, where it can mask new
warnings.

## Next action

Remove the three path properties if the SDK defaults suffice, or move
`BaseIntermediateOutputPath` to a `Directory.Build.props` that applies before the
SDK imports. Confirm the solution builds without MSB3539 and that migrations
still run from the runner.
