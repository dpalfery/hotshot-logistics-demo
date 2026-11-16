---
id: todos/migrationrunner-intermediate-output-path
title: Fix MigrationRunner MSB3539 intermediate output warning
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-16
---

# Fix MigrationRunner MSB3539 intermediate output warning

Every build of `4-Persistence/MigrationRunner/MigrationRunner.csproj` emits
MSBuild warning MSB3539. Lines 27-29 set `BaseIntermediateOutputPath`,
`IntermediateOutputPath` and `OutputPath` inside a project `<PropertyGroup>`.
`BaseIntermediateOutputPath` must be set before `Microsoft.Common.props` is
imported (for example in `Directory.Build.props`); set in the project body it is
read too late, so NuGet restore assets and other intermediate outputs can be
written to a different `obj` folder than the build reads. The paths also use a
Windows `\` separator.

The warning predates the job-status dashboard change and appears in every
baseline build, which hides new warnings in the same output.

## Next action

Remove the three output-path properties from `MigrationRunner.csproj` unless a
consumer depends on them; if one does, move `BaseIntermediateOutputPath` to a
`Directory.Build.props` beside the project. Confirm a clean `dotnet build` and
that MigrationRunner still runs its migrations from the expected output folder.
