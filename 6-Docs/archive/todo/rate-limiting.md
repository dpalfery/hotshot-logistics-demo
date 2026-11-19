---
id: todos/rate-limiting
title: Add API rate limiting
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Add API rate limiting

The [C# coding standard](../../standards/csharp/csharp.standards.md#http-pipeline) calls for rate-limiting public endpoints, but the API currently has no `AddRateLimiter()` or `UseRateLimiter()` configuration.

## Resolution

Resolved 2026-11-19. `AddApiRateLimiting()` (`RateLimiting/RateLimitingExtensions.cs`) registers `AddRateLimiter()` and
`Program` calls `UseRateLimiter()` after CORS and before authentication, as the standard orders it:

- A global fixed window per client address (`RateLimiting:GlobalPermitLimit` / `GlobalWindowSeconds`, default
  1000 per 60 s). Health endpoints are exempt from it.
- A stricter `public` policy (`PublicPermitLimit` / `PublicWindowSeconds`, default 60 per 60 s) applied with
  `[EnableRateLimiting]` to the unauthenticated actions: `GET api/Tracking/public/{jobId}` and the two payment webhooks.
- A rejected request is a 429 `ProblemDetails` with `Retry-After`. The limits are validated at startup.

The defaults are generous because the partition is the remote address and no forwarded-header handling is configured:
behind a single proxy every client shares one bucket. Configure forwarded headers (an owner decision) before lowering them.
Auth gaps found on the public endpoints are tracked in [unauthenticated endpoints](../../todo/unauthenticated-endpoints.md).

Tests: `HotshotLogistics.Api.Tests.RateLimitingTests`.
