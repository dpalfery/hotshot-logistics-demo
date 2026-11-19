---
id: todos/rate-limiting
title: Add API rate limiting
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Add API rate limiting

The [C# coding standard](../standards/csharp/csharp.standards.md#http-pipeline) calls for rate-limiting public endpoints, but the API currently has no `AddRateLimiter()` or `UseRateLimiter()` configuration.

**Next action:** Define global and endpoint-specific limits, configure the middleware, and add focused tests for throttling behavior.
