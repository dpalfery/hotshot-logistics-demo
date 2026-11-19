---
id: standards/react/rules
title: react rules
doc-type: rule
status: draft
technology: react
owner: unassigned
last-reviewed: 2026-11-19
---

# react rules

## Data states

- Every view backed by a request renders four distinct states: loading, error, empty and data.
  A count or value that has not loaded, or failed to load, is never shown as `0`, `—` or a
  default: show a skeleton while loading and an error state on failure.
- A section waits only for its own requests. Never gate a section on unrelated requests, and
  never share one "everything loaded" flag across independent widgets.
- An error message states only what is known. Do not say other data is available unless its
  own request succeeded.
- Validate every API response's shape at the service boundary before it reaches a component;
  a response that fails validation is an error state, logged once.

## Layout

- Build the approved layout exactly: grid columns, breakpoints, order and spacing. A different
  layout is a plan change raised to the planner, never an implementation choice.

## Tests

- Assert a transient state (loading, skeleton) while the response is still held back by the
  test, never after waiting for something that may already have resolved it.
- A mock change one test needs is made inside that test. Never change shared setup
  (`setupBasicMocks` or a shared flag) for one test.
