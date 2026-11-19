---
id: todos/persistence-application-dependency
title: Remove Data dependency on Application
doc-type: todo
status: resolved
component: Api
owner: unassigned
last-reviewed: 2026-11-19
---

# Remove Data dependency on Application

Clean Architecture requires inner and adapter layers not to depend on `HotshotLogistics.Application`. Currently, `4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj` has a `<ProjectReference>` to `2-Application/HotshotLogistics.Application/HotshotLogistics.Application.csproj` so `ServiceCollectionExtensions.AddCommunicationServices()` can register `CommunicationServiceFactory` (`HotshotLogistics.Application.Services`).

## Next action

Plan a small refactor to move communication service registration or `CommunicationServiceFactory` into `HotshotLogistics.Application` or `HotshotLogistics.Api`, remove the `HotshotLogistics.Application` project reference from `HotshotLogistics.Data.csproj`, and add an architecture test asserting `HotshotLogistics.Data` does not depend on `HotshotLogistics.Application`.

Resolved on 2026-11-19: `CommunicationServiceFactory` is now registered by `AddApplicationServices()`; `AddCommunicationServices()` in Data registers only the providers. The `HotshotLogistics.Application` project reference is gone from `HotshotLogistics.Data.csproj` (Data gained an explicit `Microsoft.Extensions.Http` package reference for `AddHttpClient`, which it used to receive through Application). `ArchitectureTests.Data_should_not_depend_on_application` enforces it.
