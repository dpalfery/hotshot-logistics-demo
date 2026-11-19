# HotshotLogistics.Contracts

Interfaces shared between layers. This project contains interfaces only; shared DTOs
live in `HotshotLogistics.Domain.DTOs`.
These contracts define the boundaries between layers in accordance with Clean
Architecture. The Application and Infrastructure projects reference these
interfaces so that dependencies flow inward.

## Tech Stack
- .NET 10 class library

## Build

```bash
dotnet build
```

