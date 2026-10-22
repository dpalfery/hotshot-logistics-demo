# Database setup CLI

Cross-platform .NET 8 console tool that creates the SQL Server database and application login. It does not apply FluentMigrator migrations.

**Boundary:** provisioning only. Not a runtime service. Do not embed connection strings or passwords in this repository.

**Entry point:** `HotshotLogistics.DbSetup/Program.cs`.

Detailed documentation:

- [Onboarding](../../6-Docs/dbsetup/onboarding.md)
- [Architecture](../../6-Docs/dbsetup/architecture.md)
