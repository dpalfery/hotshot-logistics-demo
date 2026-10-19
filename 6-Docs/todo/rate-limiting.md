---
id: todos/rate-limiting
title: Add API rate limiting
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Add API rate limiting

The ASP.NET Core practices rule requires rate limiting, but the API currently has no `AddRateLimiter()` or `UseRateLimiter()` configuration.

**Next action:** Define global and endpoint-specific limits, configure the middleware, and add focused tests for throttling behavior.
