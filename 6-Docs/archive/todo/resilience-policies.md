---
id: todos/resilience-policies
title: Define external-call resilience policies
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Define external-call resilience policies

The Application project references Polly, and `BillingService` configures an
`AsyncRetryPolicy` (`_retryPolicy`), but it catches broad `Exception` with no
timeout or circuit breaker. Outbound HTTP clients (`"MappingService"` in
`ServiceCollectionExtensions.AddMappingServices()`, plus notification services)
have no Polly retry, timeout, or circuit-breaker policies configured. The
[C# coding standard](../../standards/csharp/csharp.standards.md#resources) scopes these
policies to external HTTP/API calls; database retries require separate
idempotency review.

## Next action

Inventory external calls, narrow `BillingService` retry exceptions, configure
timeouts/circuit breakers on typed/named `HttpClient` registrations, and cover
cancellation and failure behavior with focused tests. Review database retry
candidates separately.

## Resolution

Resolved 2026-11-19.

**Outbound call inventory**

| Call | Transport | Resilience |
|---|---|---|
| Azure Maps and Google Maps (`"MappingService"` client) | `IHttpClientFactory` | `AddOutboundResilience()`: per-attempt and total timeout, bounded exponential retry on 5xx/408/429/network errors/attempt timeout, circuit breaker; bound from `Resilience:Outbound` |
| Stripe and PayPal processors | Simulated; take `IHttpClientFactory` but never create a client | Call the processor through `BillingService` retry (below). A real HTTP client must be registered with `AddOutboundResilience()` |
| SendGrid, Twilio, Azure Notification Hubs | Vendor SDKs with their own HTTP stack, not `IHttpClientFactory` | None added; the SDKs' own timeouts apply. Not a Polly-on-factory case; revisit if the SDK allows an injected `HttpClient` |
| Database | ADO.NET | Not retried; needs the separate transaction and idempotency review the standard requires |

**`BillingService`**: the retry no longer handles `Exception`. It retries `HttpRequestException`, `TimeoutException`,
`ExternalServiceException` and a `PaymentProcessingException` whose processor error code is listed in
`Payments:Retry:TransientErrorCodes` (default `TEMPORARY_ERROR`, `TIMEOUT`, `RATE_LIMITED`, `SERVICE_UNAVAILABLE`).
A decline or an unknown exception is not retried, because retrying a charge is not idempotent. The payment id is sent as
`idempotency_key` on every attempt, the caller's `CancellationToken` stops the backoff, and cancellation is rethrown
instead of recording a failed payment. `MaxRetries` and `BaseDelay` bind from `Payments:Retry`.

Tests: `HotshotLogistics.Tests.Resilience.OutboundResilienceTests` (fake handler),
`HotshotLogistics.Tests.Billing.PaymentRetryTests`.
