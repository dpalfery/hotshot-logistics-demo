---
id: todos/persistence-application-dependency
title: Remove Data dependency on Application
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Remove Data dependency on Application

Clean Architecture requires inner and adapter layers not to depend on `HotshotLogistics.Application`. Currently, `4-Persistence/HotshotLogistics.Data/HotshotLogistics.Data.csproj` has a `<ProjectReference>` to `2-Application/HotshotLogistics.Application/HotshotLogistics.Application.csproj` so `ServiceCollectionExtensions.AddCommunicationServices()` can register `CommunicationServiceFactory` (`HotshotLogistics.Application.Services`).

## Next action

Plan a small refactor to move communication service registration or `CommunicationServiceFactory` into `HotshotLogistics.Application` or `HotshotLogistics.Api`, remove the `HotshotLogistics.Application` project reference from `HotshotLogistics.Data.csproj`, and add an architecture test asserting `HotshotLogistics.Data` does not depend on `HotshotLogistics.Application`.
