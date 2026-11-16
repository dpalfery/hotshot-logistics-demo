---
id: todos/migration-runner-msb3539
title: Fix the MigrationRunner MSB3539 build warning
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Fix the MigrationRunner MSB3539 build warning

Every build of `4-Persistence/MigrationRunner/MigrationRunner.csproj` emits warning
MSB3539: the project sets `BaseIntermediateOutputPath` in its body `PropertyGroup`,
after the SDK has already read it, so the value is ignored. The same `PropertyGroup`
also overrides `IntermediateOutputPath` and `OutputPath` with Windows-style
`obj\` and `bin\` paths.

The warning appears in each worker's build and gate output, where it was reported
twice in one run as a baseline warning. A standing warning hides new ones, and
`BaseIntermediateOutputPath` set this late can split restore and build outputs.

## Next action

Remove the three output-path overrides from `MigrationRunner.csproj` and rely on the
SDK defaults. If a custom intermediate path is genuinely needed, set it in a
`Directory.Build.props` that is read before the SDK. Confirm the build is free of
MSB3539 and that `dotnet run --project 4-Persistence/MigrationRunner` still applies
migrations.
