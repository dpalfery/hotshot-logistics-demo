---
id: standards/data-access-layer
title: "Data access layer coding standard"
doc-type: coding-standard
status: current
technology: data-access-layer
owner: unassigned
last-reviewed: 2026-11-14
---

# Data access layer coding standard

How persistence code is written in this repository. Agents and skills resolve this
document as `<data-access-layer-coding-standard>`. Language-level C# is in the path
declared as **<csharp-coding-standard>**; SQL shape is in **<sql-coding-standard>**.
Those three must not disagree. Where they would, this document yields to them.

## Naming and layout

### Placement

| Kind | Where |
|---|---|
| Repository interface | `HotshotLogistics.Contracts` |
| Repository implementation | `HotshotLogistics.Data` |
| Persistence row | `HotshotLogistics.Data` (internal) |
| FluentMigrator scripts | `HotshotLogistics.Data/Migrations` |

Application and API code consumes `IRepository<T>`. It does not open connections or
write SQL.

## Coding standards

### Stack

- **Data access:** parameterized ADO.NET (`Microsoft.Data.SqlClient`). Dapper and Entity
  Framework are out of scope — no `QueryAsync`, no `DbContext`, no `DbSet`, no
  `SaveChangesAsync`, and no LINQ to Entities. Schema changes use FluentMigrator, not
  Entity Framework migrations.
- **Connections:** Repositories get an open connection from `ISqlConnectionFactory`
  (`HotshotLogistics.Contracts`; implemented by the internal `SqlConnectionFactory` in
  `HotshotLogistics.Data`). The caller owns the connection and disposes it with
  `await using`. Never `new SqlConnection(...)` in a repository. Migration and
  provisioning code (`MigrationRunner`, DbSetup) sits outside the factory.
- **Migrations:** FluentMigrator, versioned `[Migration(yyyyMMddHHmmss)]`. Schema design
  is not authored here — it arrives as an approved table-definition contract.
- **Abstraction:** `IRepository<T>` in `HotshotLogistics.Contracts` is the generic contract
  (get by id, get all, add, update, delete, exists). `ICustomerRepository` and
  `IPaymentRepository` extend it. The Job, Driver, JobAssignment, Invoice and
  LocationTracking contracts stay domain-specific because their signatures differ from it
  (for example Invoice `GetByIdAsync(string)` also loads line items); converging them is
  tracked in the [repository-contract todo](../../todo/repository-contract-convergence.md).
  Every implementation derives from the internal `BaseRepository<T>`, which implements
  `IRepository<T>`. Implementations are internal to `HotshotLogistics.Data`. Persistence rows
  stay in `HotshotLogistics.Data` and are mapped at the adapter boundary.

### Hard rules

- Parameterized `SqlCommand` / `SqlParameter` only. String concatenation into SQL is an
  injection, including in a migration.
- List columns explicitly. `SELECT *` is a contract with tomorrow's schema.
- Dispose connections, commands, and readers with `using` / `await using`.
- Application and repository database operations use asynchronous ADO.NET APIs and
  propagate cancellation where supported. FluentMigrator callbacks and database
  provisioning APIs may use synchronous calls when their frameworks expose no async API.
- Multi-statement work that must be atomic runs in one transaction, with an explicit
  rollback path. Do not nest transactions.
- Connection strings and credentials follow the
  [C# configuration standard](../csharp/README.md#configuration) — never a literal,
  never a committed file, never a log line.
- Register the factory as Singleton and repositories as Scoped in
  `ServiceCollectionExtensions.AddHotshotRepositories()`.
- Structured logging on failures: operation, entity, id. Do not swallow database
  exceptions.

## Testing

## Commands

```bash
dotnet build -c Release
dotnet test
```

See the [DbSetup onboarding guide](../../dbsetup/onboarding.md) for provisioning and
the [deployment pipeline documentation](../../devops/ci-cd-pipelines.md) for migrations.

## Security and data handling

### Migrations

- Use versioned FluentMigrator migrations in the format
  `[Migration(yyyyMMddHHmmss)]`; add a migration before deploying code that depends on a
  schema change.
- Make migrations individually idempotent where the platform allows it. If a migration
  cannot be safely re-run, state its preconditions and recovery steps for partial
  application.
- Keep migrations forward-only. Separate destructive schema changes from the code that
  stops using the affected data, so recovery does not depend on restoring dropped data.
- Provide a `Down()` path when the migration can be safely reversed; do not treat rollback
  as a substitute for the forward deployment sequence.

## Done checklist
