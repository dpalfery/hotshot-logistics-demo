# HotshotLogistics.Api

ASP.NET Core Web API for Hotshot Logistics. Hosting model unresolved — see `6-Docs/devops/deployment-architecture.md` (DR2).

**Boundary:** HTTP and SignalR only. Business rules live in Application/Domain. Data access is native ADO.NET (Entity Framework is prohibited).

**Entry point:** `Program.cs` in this directory. Launch URLs: `https://localhost:5001` and `http://localhost:5000`.

Detailed documentation:

- [Onboarding](../../6-Docs/api/onboarding.md)
- [Architecture](../../6-Docs/api/architecture.md)
