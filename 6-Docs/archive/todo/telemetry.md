---
id: todos/telemetry
title: Define metrics and tracing
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Define metrics and tracing

The [C# coding standard](../../standards/csharp/csharp.standards.md#safety) calls for application metrics, distributed tracing via OpenTelemetry, and Application Insights export. The API currently has no metrics/tracing registration or exporter.

## Resolution

Resolved 2026-11-19. `AddApiTelemetry()` (`Telemetry/TelemetryExtensions.cs`) registers OpenTelemetry with ASP.NET Core,
outbound `HttpClient` and .NET runtime instrumentation for traces and metrics, so request latency, error rate and
throughput (`http.server.request.duration`) and dependency calls are emitted. The resource is named by
`Telemetry:ServiceName` (default `HotshotLogistics.Api`).

Export to Application Insights uses `Azure.Monitor.OpenTelemetry.Exporter` and is configured only by settings: the
connection string is read from `Telemetry:ApplicationInsights:ConnectionString` or the `APPLICATIONINSIGHTS_CONNECTION_STRING`
variable Azure sets for an attached resource. With neither set no exporter is registered and nothing is exported. No
connection string is in the repository. `CorrelationIdMiddleware` tags the request activity with `correlation.id`, and
problem bodies carry the trace id.

Not done: SQL client and log export. Add `OpenTelemetry.Instrumentation.SqlClient` and the logs exporter if the
dashboards need them.

Tests: `HotshotLogistics.Api.Tests.TelemetryTests`.
