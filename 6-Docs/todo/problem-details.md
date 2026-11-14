---
id: todos/problem-details
title: Standardize API ProblemDetails responses
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Standardize API ProblemDetails responses

The [C# coding standard](../standards/csharp/README.md#errors) calls for `AddProblemDetails()`, `UseExceptionHandler()`, and consistent `ProblemDetails` / `ValidationProblem` responses, whereas controllers currently return mixed plain strings, anonymous objects, and middleware-generated errors.

**Next action:** Define the error contract and status-code mapping, standardize controller and middleware responses, and add focused API tests.