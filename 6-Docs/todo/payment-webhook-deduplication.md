---
id: todos/payment-webhook-deduplication
title: De-duplicate payment webhook events
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# De-duplicate payment webhook events

The Stripe and PayPal webhook actions verify the provider signature and a five-minute timestamp window, but a verified
event can still be replayed inside that window, and providers redeliver events on their own retries. Nothing records which
provider event ids were already processed, so the same payment event can update a payment twice. This was recorded as
not done when the [unauthenticated endpoints todo](../archive/todo/unauthenticated-endpoints.md) was resolved.

## Next action

Store the provider's event id (Stripe `id`, PayPal `id`) when an event is processed, with a unique constraint, and skip an
event whose id is already stored (answer 200 so the provider stops retrying). The storage needs a table and migration, so
it follows the [Payments table](payments-table.md) work. Test: the same signed event posted twice changes the payment once.
