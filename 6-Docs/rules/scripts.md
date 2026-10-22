---
id: rules/scripts
title: Scripts Rule
doc-type: rule
status: current
owner: unassigned
last-reviewed: 2026-10-20
---

# Scripts Rule

This rule provides common command-line scripts and commands used in the Hotshot Logistics .NET project for development, building, testing, and database management.

## When to Apply
Use these commands whenever working with the .NET project for development tasks, builds, testing, or database operations.

## Development Commands

### Hot Reload Development
- `dotnet watch` - Start the application with hot reload for development
- `dotnet watch run` - Run the application with file watching and automatic restart

### Building
- `dotnet build` - Build the project in debug mode
- `dotnet build -c Release` - Build the project in release mode for production

### Running
- `dotnet run` - Run the application locally
- `dotnet run --launch-profile "Development"` - Run with specific launch profile

### Testing
- `dotnet test` - Run all unit and integration tests
- `dotnet test --filter "Category=Unit"` - Run only unit tests
- `dotnet test --filter "Category=Integration"` - Run only integration tests

## Database Provisioning

Run the supported DbSetup CLI; it provisions the SQL Server database, login, and application user and does not run migrations — `MigrationInvoker` is implemented but is dead code, never invoked by `Program`. The deploy pipeline applies schema with `dotnet fm migrate`. See the [DbSetup onboarding guide](../dbsetup/onboarding.md) for required options and secret-handling rules.

```bash
dotnet run --project 7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj -- --server "<sql-server-instance>" --db-name "hotshot_logistics"
```

## Package Management
- `dotnet add package PackageName` - Add a NuGet package to the project
- `dotnet remove package PackageName` - Remove a NuGet package
- `dotnet restore` - Restore all NuGet packages

## Code Generation
- `dotnet new` - Create new projects, files, or solutions
- `dotnet new sln` - Create a new solution file
- `dotnet sln add Project.csproj` - Add a project to the solution

## Cleaning
- `dotnet clean` - Clean the build output
- `dotnet nuget locals all --clear` - Clear all NuGet caches
