---
id: todos/api-platform-hardening
title: Adopt sealed record HTTP DTOs
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Adopt sealed record HTTP DTOs

The [C# coding standard](../standards/csharp/csharp.standards.md#stack) targets `sealed record` request and response DTOs for controller actions. Currently, `0` `record` types exist across `0-Base/`–`4-Persistence/`.

## Next action

Plan migrating HTTP request and response DTOs to `sealed record` types (coordinated with [contract model placement](contracts-model-placement.md)), preserving JSON serialization and existing controller contract tests.
