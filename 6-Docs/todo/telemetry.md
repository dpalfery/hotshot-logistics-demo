---
id: todos/telemetry
title: Define metrics and tracing
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Define metrics and tracing

The [C# coding standard](../standards/csharp/README.md#safety) calls for application metrics, distributed tracing via OpenTelemetry, and Application Insights export. The API currently has no metrics/tracing registration or exporter.

**Next action:** Select the telemetry provider and exporter, define required metrics and trace context, assess Application Insights, then implement focused instrumentation and tests.