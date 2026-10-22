---
id: api/onboarding
title: API onboarding
doc-type: onboarding
status: current
component: Api
source-root: 1-Presentation/HotshotLogistics.Api
owner: unassigned
last-reviewed: 2026-10-20
---

# API onboarding

## Dependencies

- .NET 8 SDK
- SQL Server (local, Docker, or remote), provisioned with [DbSetup](../dbsetup/onboarding.md)
- Solution restore from the repository root: `dotnet restore HotshotLogistics.sln`

## Setup

1. Build: `dotnet build 1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj`
2. Set local configuration with **.NET user secrets**. Do not write secrets into files under the repository.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project 1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj
```

Optional Azure App Configuration (either key). If neither is set, user secrets are used as-is:

```bash
dotnet user-secrets set "AppConfiguration:Endpoint" "https://<store-name>.azconfig.io" --project 1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj
```

3. Run: `dotnet run --project 1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj`

Launch profile URLs are `https://localhost:5001` and `http://localhost:5000` ([launchSettings.json](../../1-Presentation/HotshotLogistics.Api/Properties/launchSettings.json)). In Development, Swagger UI is enabled and authentication uses the in-process test handler.

## Debug path

- Development authentication is `TestAuthHandler` (scheme `Test`), not Azure AD B2C.
- Non-development uses JWT Bearer via `AzureAdB2C` configuration from App Configuration / Key Vault.
- CORS origins come from `Cors:AllowedOrigins`.
- SignalR hub is mapped at `/realtime`.

## Non-standard operating procedures

- Entity Framework is prohibited. Schema changes go through FluentMigrator in Persistence; the deploy pipeline applies them with `dotnet fm migrate`. DbSetup provisions the database and login only.
- Agents must not run `terraform apply`, Docker builds, or ACR pushes. See the root `AGENTS.md`.
