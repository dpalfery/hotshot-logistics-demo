---
id: todos/unauthenticated-endpoints
title: Review endpoints that are unauthenticated or unreachable by their callers
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Review endpoints that are unauthenticated or unreachable by their callers

Found while adding rate limiting (see [rate limiting](../archive/todo/rate-limiting.md)); changing it alters who can call
the API, so it needs an owner decision.

- `TrackingController` has no `[Authorize]`. Every action, including `start`, `stop`, `location` updates and
  `route-deviation`, is anonymous; only `public/{jobId}` is meant to be. Recommendation: put `[Authorize]` on the class
  and `[AllowAnonymous]` on `public/{jobId}`, then update the driver clients that call it.
- `BillingController` is `[Authorize]` at class level, so the Stripe and PayPal webhook actions reject the providers'
  unauthenticated calls with 401. Recommendation: `[AllowAnonymous]` on the two webhook actions, with the signature
  check in `IPaymentProcessor.ProcessWebhookAsync` as the authentication.
- The `Public` rate limit (`RateLimiting:PublicPermitLimit`) already applies to `public/{jobId}` and both webhooks.

**Next action:** Decide the two recommendations, then add tests that prove the anonymous and authenticated behaviour.
