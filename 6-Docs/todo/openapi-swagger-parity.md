---
id: todos/openapi-swagger-parity
title: Investigate OpenAPI and Swashbuckle parity
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Investigate OpenAPI and Swashbuckle parity

The API currently uses Swashbuckle (`AddSwaggerGen`, `UseSwagger`, and `UseSwaggerUI`). The ASP.NET Core rule also names `Microsoft.AspNetCore.OpenApi`, but migration is deferred because feature parity is not yet established.

**Next action:** Compare required Swagger/OpenAPI features with the current Microsoft package support, then decide whether to retain Swashbuckle or migrate.
