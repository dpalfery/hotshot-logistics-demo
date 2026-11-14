---
id: todos/azure-configuration-support
title: Complete Azure configuration support
doc-type: todo
status: current
component: Api
owner: unassigned
last-reviewed: 2026-11-14
---

# Complete Azure configuration support

The C# standard calls for local non-secret settings in appsettings files and
local secrets in .NET User Secrets. Applications deployed to Azure must support
Azure App Configuration and Key Vault references.

The API currently adds App Configuration only when configured and prefers a
connection string when one is present. The App Configuration setup does not
explicitly configure Key Vault references.

## Next action

Plan and verify the configuration flow for local development and Azure
deployment. Include managed identity, Key Vault-reference resolution, startup
failure behavior, and focused tests. Review required infrastructure changes
separately before implementation.
