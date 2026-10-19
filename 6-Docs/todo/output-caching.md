---
id: todos/output-caching
title: Evaluate API output caching
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Evaluate API output caching

The optional-features rule describes output/response caching, but the API currently has no caching middleware. It is not yet established which GET endpoints are safe to cache or whether a distributed Redis provider is needed.

**Next action:** Identify cacheable endpoints, define invalidation and freshness requirements, choose in-memory versus Redis storage, then implement focused tests if caching is approved.