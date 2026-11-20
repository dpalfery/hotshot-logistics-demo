---
id: todos/openapi-swagger-parity
title: Investigate OpenAPI and Swashbuckle parity
doc-type: todo
status: superseded
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Investigate OpenAPI and Swashbuckle parity

Declined/closed on 2026-11-14: the API continues to use `Swashbuckle.AspNetCore` (`AddSwaggerGen` / `UseSwagger` / `UseSwaggerUI`) per [C# coding standard](../../standards/csharp/csharp.standards.md#http-pipeline).

The API currently uses Swashbuckle (`AddSwaggerGen`, `UseSwagger`, and `UseSwaggerUI`). The [C# coding standard](../../standards/csharp/csharp.standards.md#http-pipeline) also targets `Microsoft.AspNetCore.OpenApi`, and migration is deferred until feature parity is established.

**Next action:** Compare required Swagger/OpenAPI features with the current Microsoft package support, then decide whether to retain Swashbuckle or migrate.
