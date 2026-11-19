---
id: standards/csharp/rules
title: csharp rules
doc-type: rule
status: draft
technology: csharp
owner: unassigned
last-reviewed: 2026-11-19
---

# csharp rules

## HTTP endpoints

- A new route must not shadow an existing one. A literal segment beside a parameter route
  (`jobs/{id}`) needs a constrained parameter (`{id:int}`, `{id:guid}`) or a distinct path, and a
  routing test proving both routes still resolve to the right action.
- A change to a shared response contract (a type more than one client reads) is called out in
  the plan's design and keeps every existing consumer working; narrowing it is a breaking change.
