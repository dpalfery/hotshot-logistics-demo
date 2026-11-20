---
id: todos/unauthenticated-endpoints
title: Review endpoints that are unauthenticated or unreachable by their callers
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Review endpoints that are unauthenticated or unreachable by their callers

Found while adding rate limiting (see [rate limiting](./rate-limiting.md)); changing it alters who can call
the API, so it needs an owner decision.

- `TrackingController` had no `[Authorize]`. Every action, including `start`, `stop`, `location` updates and
  `route-deviation`, was anonymous; only `public/{jobId}` is meant to be.
- `BillingController` is `[Authorize]` at class level, so the Stripe and PayPal webhook actions rejected the providers'
  unauthenticated calls with 401. The processors' signature checks were placeholders (a Stripe header longer than ten
  characters, a PayPal header containing `paypal`), so simply allowing anonymous access would have let anyone post
  forged payment events.
- The `Public` rate limit (`RateLimiting:PublicPermitLimit`) already applies to `public/{jobId}` and both webhooks.

## Resolution

Resolved 2026-11-19; the [API architecture](../../api/architecture.md#authorization) states the resulting behaviour.

- `TrackingController` is `[Authorize]` at class level. `start`, `stop/{jobId}`, `location` and `check-deviation` need
  the `ManagerOrDriver` policy; `location/{jobId}` and `history/{jobId}` need `ManagerOrAdmin`; `public/{jobId}` is
  `[AllowAnonymous]` and returns `PublicTrackingInfo`, which carries no driver id, internal id, speed or notes.
- The two webhook actions are `[AllowAnonymous]` and are authenticated by the provider signature, verified inside
  `IPaymentProcessor.ProcessWebhookAsync` before anything is read from the payload or processed. A request that fails
  verification gets 401 and `SignatureRejected` is set on the result. Stripe: `Stripe-Signature` HMAC-SHA256 over
  `timestamp.payload` with `Stripe:WebhookSecret`, five-minute tolerance. PayPal: `PAYPAL-TRANSMISSION-SIG` RSA-SHA256
  over `id|time|webhookId|crc32(body)` with the certificate at `PAYPAL-CERT-URL` (fetched only from `*.paypal.com`,
  chain-validated), webhook id from `PayPal:WebhookId`, five-minute tolerance.
- Tests: `HotshotLogistics.Api.Tests.TrackingAuthorizationTests`, `HotshotLogistics.Api.Tests.PaymentWebhookTests`,
  `HotshotLogistics.Tests.Billing.PayPalCertificateProviderTests`.

Not done: replay protection beyond the timestamp window (no event-id de-duplication), and the public tracking page
still reports a fixed `In Transit` status and an estimated arrival two hours after the last fix because the job status
is not looked up.

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
