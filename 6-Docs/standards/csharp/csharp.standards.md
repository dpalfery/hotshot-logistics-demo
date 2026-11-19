---
id: standards/csharp
title: "C# coding standard"
doc-type: coding-standard
status: current
technology: csharp
owner: unassigned
last-reviewed: 2026-11-14
---

# C# coding standard

How C# is written in this repository. Agents and skills resolve this document as
`<csharp-coding-standard>`, so it outranks the defaults a portable agent shipped with.

## Naming and layout

### Style

`.editorconfig` is the source of truth. The choices worth stating because other .NET
repositories disagree:

- **Explicit types, never `var`** — including where the type is apparent.
  Encode this in `csharp_style_var_*`.
- **Block-scoped namespaces.**
- **Predefined type keywords** — `string`, `int`, `bool`, never `String` or `Int32`.
- **Allman braces**, and a new line before `else`, `catch` and `finally`.

### Naming

C# follows the official **.NET Framework Design Guidelines**. Casing communicates
scope, accessibility, and kind of identifier at a glance. Encode mechanical naming
rules in `.editorconfig` where an analyzer exists; the table below is the policy
agents and reviewers apply when the analyzer is silent.

| Identifier type | Casing | Prefix / suffix | Example |
|---|---|---|---|
| Local variables | `camelCase` | None | `userCount`, `isValid` |
| Method parameters | `camelCase` | None | `userId`, `cancellationToken` |
| Private fields (instance) | `_camelCase` | `_` prefix | `_retryCount`, `_logger` |
| Private fields (static) | `_camelCase` (or `s_camelCase`) | `_` or `s_` prefix | `_defaultTimeout`, `s_instance` |
| Public / protected fields | `PascalCase` | None | `MaxValue`, `DefaultPort` |
| Constants (`const`) | `PascalCase` | None | `MaxRetries`, `ConnectionTimeout` |
| Properties | `PascalCase` | None | `FirstName`, `IsActive` |
| Interfaces | `PascalCase` | `I` prefix | `IRepository`, `IDisposable` |
| Generic type parameters | `PascalCase` | `T` prefix | `T`, `TKey`, `TEntity` |

#### Core variable and field rules

- **Local variables and parameters:** always `camelCase`. Avoid abbreviations unless
  they are standard domain acronyms (for example `id`, `xml`, `api`).
- **Private instance fields:** `_camelCase` with a leading underscore so fields are
  distinct from locals without requiring `this.`.
- **Constants and static readonly values:** `PascalCase` for `const` and
  `static readonly`. Do **not** use uppercase `SNAKE_CASE` (use `MaxBufferSize`, not
  `MAX_BUFFER_SIZE`).
- **Boolean identifiers:** affirmative verb prefix that states a true/false condition:
  `hasErrors`, `canExecute`, `isVisible`, `shouldRetry`.

#### Naming conventions and suffixes

- **Async methods:** suffix methods that return `Task` or `Task<T>` with `Async`
  (for example `FetchDataAsync`).
- **Events and delegates:** verbs or verb phrases in `PascalCase` (for example
  `Closing`, `DataReceived`). Suffix custom event-argument classes with `EventArgs`
  (for example `OrderPlacedEventArgs`).
- **Exceptions:** suffix custom exception classes with `Exception` (for example
  `NotFoundException`).
- **Attributes:** suffix custom attribute classes with `Attribute` (for example
  `JsonSerializableAttribute`).

#### Anti-patterns

- **Hungarian notation:** never encode the data type in the name (use `count`, not
  `iCount`; `customers`, not `arrCustomers`; `name`, not `strName`).
- **Single-letter names:** avoid except for standard loop counters (`i`, `j`),
  coordinate axes (`x`, `y`), or lambdas where type and intent are obvious
  (`x => x.Id`).
- **Screaming snake case:** never use `ALL_CAPS_WITH_UNDERSCORES` for constants or
  enum members in modern C#.

### Model classification and placement

A database key, a set of public auto-properties, a default initializer, an attribute, or
a property count does not make a type an Entity.

| Kind | Where | Shape |
|---|---|---|
| Shared DTO | `HotshotLogistics.Domain/DTOs` (namespace `HotshotLogistics.Domain.DTOs`) | Property bag, no domain invariant. Suffix `Dto`. |
| Use-case DTO | `HotshotLogistics.Application` | Same shape, same `Dto` suffix, not shared across layers. |
| Entity | `HotshotLogistics.Domain/Entities` | Stable identity plus a business invariant or legal state transition. Controlled construction and mutation; no getter/setter-only padding. |
| Value object | `HotshotLogistics.Domain/ValueObjects` | Immutable, equality by value. May carry domain behaviour; has no independent identity. |
| Persistence row | `HotshotLogistics.Data` (internal) | Storage/schema projection. Mapped at the adapter boundary. Not a shared contract and not a Domain entity. |

`HotshotLogistics.Contracts` contains interfaces only. Shared DTOs live in
`HotshotLogistics.Domain.DTOs` (owner decision, 2026-11-19); there is no separate contracts
models project. Any exception is an architecture decision with focused behaviour tests, not a
convenience.

Server-side C# follows Clean Architecture: inner layers never depend on outer layers;
business invariants belong in Domain; application services belong in
`HotshotLogistics.Application/Services`.

Do not add getter/setter-only tests to pad coverage.

## Coding standards

### Stack

- **Target:** ASP.NET Core on .NET 10, using its default C# 14 language version.
- **HTTP surface:** MVC controllers (`ControllerBase`), not minimal APIs. Use MVC
  `ActionResult` / `ActionResult<T>` and `ControllerBase` helpers. Target request and
  response bodies are `sealed record` DTOs (tracked in the
  [sealed-record DTOs todo](../../todo/api-platform-hardening.md)); never return a
  domain entity or persistence row from an action.
- **Data access:** parameterized ADO.NET in the persistence project. Dapper and Entity
  Framework are out of scope. FluentMigrator owns schema change.
- **Composition:** constructor DI. Configuration follows the Options pattern.

Application and API code consumes `IRepository<T>` (or an equivalent persistence
abstraction). It does not open connections, write SQL, or author migrations.

### Configuration

- Bind settings with the Options pattern: `IOptions<T>`, `IOptionsSnapshot<T>` for per-request reload, `IOptionsMonitor<T>` for change notification. Never hardcode configuration values.
- For local development, use `appsettings.json` and `appsettings.Development.json` for non-secret settings.
- Store local development secrets in .NET User Secrets outside the repository. Never use production secrets for development or testing.
- Applications deployed to Azure must support Azure App Configuration and Key Vault references for settings and secrets.
- Follow Microsoft Learn: [safe storage of app secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0) and [Key Vault references](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-key-vault-references-dotnet-core).

### Types

**Seal by default.** A public type is `sealed` unless something derives from it.
Inheritance is a deliberate extension point, not the default shape of a class.

**Records for values, classes for behaviour.** A record models a value the code passes
around and compares. Something with dependencies or lifetime is a class. Request and
response DTOs are `sealed record`.

**Nullable is enabled.** A nullable annotation is a claim; do not answer a warning with
`!` when the honest fix is a check or a different signature.

One top-level type per file, named after the file. Align filename, namespace, and suffix
with the classification below.

### Async

- Methods that return a `Task` take the `Async` suffix.
- Async all the way down. `.Result`, `.Wait()`, and `Thread.Sleep` in async code are
  blocking calls; use `await` and `Task.Delay`.
- Accept a `CancellationToken` on I/O — including controller actions — and pass it
  through. A call that cannot be cancelled is a hang waiting for a bad network.

### Resources

`IDisposable` values are owned. Dispose them with `using` / `await using`, or the type
that holds them implements `IDisposable` and disposes them. A leaked `HttpClient`,
stream, or connection is not a later problem — it is a production incident.

Reuse outbound HTTP through `IHttpClientFactory`. Configure retries, timeouts, and
circuit breaking on the factory with Polly; do not new up `HttpClient` or wrap every
call site in its own policy.

- Use typed clients. Give every outbound call a timeout and pass the `CancellationToken`.
- Retry only transient failures, with exponential backoff and a bounded attempt count.
- Circuit breakers fail fast during prolonged outages. Log retry and breaker events.
- Database retries need a separate review of transaction boundaries and idempotency.

### Errors

- **Catch what you can name.** Operational failures are caught by type and translated
  into a diagnostic, a log line, an exit code, or RFC 7807 `ProblemDetails`. A bare
  `catch (Exception)` around code that can fail for a reason you have not thought
  about hides the reason.
- **Never swallow.** A failure that produces no diagnostic, no log line and no exit
  code is indistinguishable from success. If swallowing is genuinely intended, the
  comment says why.
- Rethrow with `throw;`, never `throw ex;`.
- Use MVC `ValidationProblem` / `ProblemDetails` for invalid input, not a bare
  string. The API registers `AddProblemDetails()`, a domain `IExceptionHandler` and
  `UseExceptionHandler()` / `UseStatusCodePages()`; controllers return the same body through the
  `ApiControllerBase` helpers. Never add a catch-all `catch (Exception)` that returns 500: let it
  reach the handler.
- Validate every endpoint input on the server even when the client validates. Use data
  annotations or FluentValidation; validate file type and size on uploads. Return 400
  for validation failures and never leak internals in an error body.

### HTTP pipeline

Middleware order is:

`UseHttpsRedirection` → `UseCors` → `UseRateLimiter` → `UseAuthentication` →
`UseAuthorization` → `UseOutputCaching` / `UseResponseCaching` → endpoints.

Version the API. OpenAPI is served with Swashbuckle (`AddSwaggerGen` / `UseSwagger` /
`UseSwaggerUI`).
New and modified public API members carry XML documentation comments.

Target: expose `/health` with checks for the dependencies this process needs (database,
queue, downstream API) so an orchestrator can tell liveness from readiness (tracked in
the [health-checks todo](../../todo/health-checks.md)).

Cache responses only where the data is safe to reuse. Rate-limit public endpoints (see the
[rate-limiting todo](../../todo/rate-limiting.md)). Measure with the platform diagnostics
rather than guessing.

### Dependencies

New dependencies need a reason: what it does, why the existing stack cannot, and who
maintains it. This repository's data stack is ADO.NET plus FluentMigrator — do not
import Dapper, Entity Framework, or a second migrator because a portable skill
shipped with them.

## Testing

### Tests

Test authorship follows `<test-coding-standard>`. Language-level C# in test files
follows this document. New behaviour ships with the test that would fail without it.

## Commands

```bash
dotnet watch              # hot reload
dotnet build -c Release   # production build
dotnet test               # test run
dotnet run                # local run
```

Database provisioning: see the [DbSetup onboarding guide](../../dbsetup/onboarding.md).

### The build is the first reviewer

`.editorconfig` and the analyzers are authoritative for formatting and mechanical rules.
`TreatWarningsAsErrors` is on with `AnalysisMode=Default`. A warning is a failed build.
Adding an id to `NoWarn` is a repository-wide decision and needs a reason you can state
in the file (`Directory.Build.props` currently suppresses `CS1591` while `GenerateDocumentationFile`
is enabled for `IDE0005` until a repository-wide XML-doc pass is completed).

Do not merge code that fails the build, and do not restate analyzer rules here.

## Security and data handling

### Safety

- Parameterized ADO.NET commands, always. String concatenation into SQL is an
  injection, including in a migration.
- Follow the configuration policy above; never put secrets in literals, repository files,
  or logs.
- Log through `ILogger<T>` with structured properties and a correlation id.
  `Console.WriteLine` is not production logging. Configure providers per environment.
- Middleware creates or accepts the correlation id and propagates it to outbound calls,
  logs, traces, and `ProblemDetails` responses.
- Target: emit metrics (latency, error rate, throughput) and distributed traces to
  Application Insights through OpenTelemetry (tracked in the
  [telemetry todo](../../todo/telemetry.md)).
- HTTPS and HSTS are on. Authenticate, then authorize. CORS is explicit. CSRF
  protection is on wherever cookie-authenticated browser clients can POST. Persist
  Data Protection keys and rotate them.

## Done checklist
