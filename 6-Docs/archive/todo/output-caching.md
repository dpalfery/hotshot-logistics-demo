---
id: todos/output-caching
title: Evaluate API output caching
doc-type: todo
status: superseded
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Evaluate API output caching

Declined/closed on 2026-11-14: operational logistics endpoints serve live auth-scoped state and no read bottleneck requires response caching. Reopen if a measured read bottleneck appears.

The [C# coding standard](../../standards/csharp/csharp.standards.md#http-pipeline) covers output and response caching, but the API currently has no caching middleware. It is not yet established which GET endpoints are safe to cache or whether a distributed Redis provider is needed.

**Next action:** Identify cacheable endpoints, define invalidation and freshness requirements, choose in-memory versus Redis storage, then implement focused tests if caching is approved.