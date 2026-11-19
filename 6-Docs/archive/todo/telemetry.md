---
id: todos/telemetry
title: Define metrics and tracing
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Define metrics and tracing

The [C# coding standard](../../standards/csharp/README.md#safety) calls for application metrics, distributed tracing via OpenTelemetry, and Application Insights export. The API currently has no metrics/tracing registration or exporter.

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

## Decisions

| ID | Question | Answer | Status |
|---|---|---|---|
| D1 | Where does the durable record of this completed todo live? | The resolution in this archived todo is the record. | Approved |

## Closeout mapping

| ID | Destination |
|---|---|
| D1 | Documentation waiver — the resolution in this archived todo is the lasting record |

## Design harvest

Documentation waiver — nothing survives because the completed work is recorded in this archived todo and canonical docs were updated when the work landed.
