---
id: standards/csharp/rules
title: csharp rules
doc-type: rule
status: draft
owner: unassigned
last-reviewed: 2026-11-19
technology: csharp
---

# csharp rules

## Scope

- A repository, SQL or migration change is written to the `data-access-layer` standard,
  not to this one. When a task needs a schema change it does not name, finish the C# part
  and report the need as an open item, naming the table and columns.

## HTTP endpoints

- A new route must not shadow an existing one. A literal segment beside a parameter route
  (`jobs/{id}`) needs a constrained parameter (`{id:int}`, `{id:guid}`) or a distinct path, and a
  routing test proving both routes still resolve to the right action.
- A change to a shared response contract (a type more than one client reads) is called out in
  the plan's design and keeps every existing consumer working; narrowing it is a breaking change.
