---
id: todos/problem-details
title: Standardize API ProblemDetails responses
doc-type: todo
status: superseded
documentation-impact: no-design-change
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Standardize API ProblemDetails responses

The [C# coding standard](../../standards/csharp/README.md#errors) calls for `AddProblemDetails()`, `UseExceptionHandler()`, and consistent `ProblemDetails` / `ValidationProblem` responses, whereas controllers currently return mixed plain strings, anonymous objects, and middleware-generated errors.

## Resolution

Resolved 2026-11-19. Every error path now answers with an RFC 9457 `ProblemDetails` body
(`application/problem+json`) carrying `traceId` and `correlationId`:

- `AddProblemDetails()`, `AddExceptionHandler<DomainExceptionHandler>()`, `UseExceptionHandler()` and
  `UseStatusCodePages()` (`Errors/ErrorHandlingExtensions.cs`) cover unhandled exceptions and bodiless
  401/403/404/405 responses.
- `DomainExceptionHandler` maps `ValidationException` to 400 `ValidationProblemDetails`,
  `BusinessRuleException` to 422, `PaymentProcessingException` to 402 and `ExternalServiceException` to 502.
  Any other exception is a 500 whose body carries no message or stack trace.
- Controllers inherit `ApiControllerBase`; its `BadRequestProblem`, `NotFoundProblem`, `ConflictProblem` and
  `ValidationFailed` helpers keep the MVC result types and use the same body. Per-action catches of named
  exceptions stay, so a status code did not change; the catch-all 500 blocks were removed.
- `CorrelationIdMiddleware` accepts or creates `X-Correlation-ID`. The unused `ExceptionHandlingMiddleware` and
  `ErrorResponse` were removed.

Tests: `HotshotLogistics.Api.Tests.ProblemDetailsTests`.

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
