---
id: todos/resilience-policies
title: Define external-call resilience policies
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Define external-call resilience policies

The Application project references Polly, and `BillingService` configures an
`AsyncRetryPolicy` (`_retryPolicy`), but it catches broad `Exception` with no
timeout or circuit breaker. Outbound HTTP clients (`"MappingService"` in
`ServiceCollectionExtensions.AddMappingServices()`, plus notification services)
have no Polly retry, timeout, or circuit-breaker policies configured. The
[C# coding standard](../standards/csharp/csharp.standards.md#resources) scopes these
policies to external HTTP/API calls; database retries require separate
idempotency review.

## Next action

Inventory external calls, narrow `BillingService` retry exceptions, configure
timeouts/circuit breakers on typed/named `HttpClient` registrations, and cover
cancellation and failure behavior with focused tests. Review database retry
candidates separately.