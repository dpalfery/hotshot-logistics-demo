---
id: todos/api-platform-hardening
title: Adopt sealed record HTTP DTOs
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Adopt sealed record HTTP DTOs

The [C# coding standard](../../standards/csharp/csharp.standards.md#stack) targets `sealed record` request and response DTOs for controller actions. Currently, `0` `record` types exist across `0-Base/`–`4-Persistence/`.

## Resolution

Resolved 2026-11-19 for the request and response types the controllers define. The 18 classes that lived at the bottom of
the controller files are now `sealed record` types with `init` properties, one per file under
`1-Presentation/HotshotLogistics.Api/Models/` (namespace `HotshotLogistics.Api.Models`), and the controllers are sealed.
Type names are unchanged, so the OpenAPI schema names are too. `HotshotLogistics.Api.Tests.DtoWireShapeTests` round-trips a
golden JSON document for each type through the MVC Newtonsoft serializer settings of the running host, and it passed
against the old classes before the conversion, so the wire shape is unchanged (these are characterisation tests, not
red-first).

Left open in [entities and shared DTOs in actions](../../todo/entities-in-controller-actions.md): actions that still take
or return domain entities and the mutable shared DTOs in `Domain.DTOs`.

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
