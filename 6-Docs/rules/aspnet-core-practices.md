---
id: rules/aspnet-core-practices
title: ASP.NET Core Practices Rule
doc-type: rule
status: current
owner: unassigned
last-reviewed: 2026-11-09
---

# ASP.NET Core Practices Rule

This rule enforces best practices for ASP.NET Core applications in the Hotshot Logistics project, ensuring secure, performant, and maintainable services.

## When to Apply
Apply these practices whenever developing or modifying ASP.NET Core APIs, controllers, or services in .NET 10 projects (every project in this solution targets `net10.0`).

## Configuration
- Centralize settings using the **Options pattern** with dependency injection.
- Use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` for configuration binding.
- Override settings via `appsettings.{Environment}.json` and environment variables.
- Never hardcode configuration values.

## Logging
See the [observability rule](observability.md) for structured logging, correlation IDs, logging providers, and log-level guidance.

## API Documentation
- Generate OpenAPI specifications using `Microsoft.AspNetCore.OpenApi`.
- Provide Swagger UI via Swashbuckle for interactive documentation.
- Version APIs using URL versioning or header versioning.
- Include XML documentation comments on public APIs.

## Middleware Order
Configure middleware in the correct order for security and functionality:
1. `UseHttpsRedirection` - Redirect HTTP to HTTPS
2. `UseCors` - Enable Cross-Origin Resource Sharing
3. `UseAuthentication` - Authenticate requests
4. `UseAuthorization` - Authorize requests
5. Endpoint routing

## Performance Practices
- Use async/await for all I/O operations.
- Reuse HttpClient instances via `IHttpClientFactory`.
- See the [optional features rule](optional-features.md) for rate limiting and output/response caching guidance.
- Measure performance with diagnostics and Application Insights.

## Health Checks
- Implement `/health` endpoints with database, queue, and API checks.
- Integrate with orchestrators like Kubernetes for readiness/liveness probes.
- Use `Microsoft.AspNetCore.Diagnostics.HealthChecks` package.

## References
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration)
- [ASP.NET Core Logging](https://learn.microsoft.com/aspnet/core/fundamentals/logging)
- [ASP.NET Core OpenAPI](https://learn.microsoft.com/aspnet/core/fundamentals/openapi)
- [ASP.NET Core Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
