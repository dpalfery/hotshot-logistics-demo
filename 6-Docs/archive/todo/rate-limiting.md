---
id: todos/rate-limiting
title: Add API rate limiting
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Add API rate limiting

The [C# coding standard](../../standards/csharp/README.md#http-pipeline) calls for rate-limiting public endpoints, but the API currently has no `AddRateLimiter()` or `UseRateLimiter()` configuration.

## Resolution

Resolved 2026-11-19. `AddApiRateLimiting()` (`RateLimiting/RateLimitingExtensions.cs`) registers `AddRateLimiter()` and
`Program` calls `UseRateLimiter()` after CORS and before authentication, as the standard orders it:

- A global fixed window per client address (`RateLimiting:GlobalPermitLimit` / `GlobalWindowSeconds`, default
  1000 per 60 s). Health endpoints are exempt from it.
- A stricter `public` policy (`PublicPermitLimit` / `PublicWindowSeconds`, default 60 per 60 s) applied with
  `[EnableRateLimiting]` to `GET api/Tracking/public/{jobId}` and the two payment webhooks. The other `TrackingController`
  actions are anonymous too but get only the global limit; whether to throttle driver location updates is an owner decision.
- A rejected request is a 429 `ProblemDetails` with `Retry-After`. The limits are validated at startup.

The defaults are generous because the partition is the remote address and no forwarded-header handling is configured:
behind a single proxy every client shares one bucket. Configure forwarded headers (an owner decision) before lowering them.
Auth gaps found on the public endpoints are tracked in [unauthenticated endpoints](unauthenticated-endpoints.md).

Tests: `HotshotLogistics.Api.Tests.RateLimitingTests`.

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
