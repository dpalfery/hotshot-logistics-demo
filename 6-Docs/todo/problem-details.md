---
id: todos/problem-details
title: Standardize API ProblemDetails responses
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Standardize API ProblemDetails responses

The validation rule requires consistent ProblemDetails responses, but controllers currently return mixed plain strings, anonymous objects, and middleware-generated ProblemDetails-style errors.

**Next action:** Define the error contract and status-code mapping, standardize controller and middleware responses, and add focused API tests.