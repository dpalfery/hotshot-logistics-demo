---
id: todos/resilience-policies
title: Define external-call resilience policies
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Define external-call resilience policies

The Application project references Polly, but no retry, timeout, circuit-breaker, or resilience-pipeline registration is configured. The resilience rule now scopes these policies to external HTTP/API calls; database retries require separate idempotency review.

**Next action:** Inventory external calls, define retryable failures, timeouts, circuit-breaker thresholds, cancellation behavior, and focused tests. Review database retry candidates separately.