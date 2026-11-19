---
id: standards/test/rules
title: test rules
doc-type: rule
status: draft
technology: test
owner: unassigned
last-reviewed: 2026-11-19
---

# test rules

## Test data

- Every row a test creates uses an identifier generated for that test run. Never use a fixed id
  that existing data or another test may already hold.
- A test cleans up only rows it created, and registers a row for cleanup only after the create
  succeeded. Never delete by an id the test did not create.
- Assert exact values against data the test owns (its own unique customer, tenant or prefix),
  not deltas against shared data that other tests or seeds can change.

## Assertions

- Assert the specific refusal or exception the code must raise, never "any exception".
- Wait for the condition under test, never for an unrelated event, and never assert a state that
  may already have passed.
- New logic ships with tests that exercise each of its branches, including the failure branch.
