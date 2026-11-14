# AGENTS.md

## Project Overview
**bolt-hotshot-logistics** - A comprehensive cloud-native logistics platform designed to modernize hotshot delivery operations through automated, real-time technology.

## Non-negotiable rules

`AGENTS.md` files are mandatory instructions, not optional background material. Read this file before work anywhere in the repository, then read the nearest scoped `AGENTS.md` before changing files in that subtree.

- Do not create infrastructure, deployment assets, dependencies, cross-cutting concerns, or governed documentation files under `6-Docs/` without user approval. The exception is plans, specifications, and todos that `architect` or `product-owner` write through the conductor workflow under the directories declared as **<plan-index>**, **<specification-index>**, and **<todo-index>**; the conductor's intake and approve-and-execute gates are their approval, and agents must not forbid or skip those writes. Ask before an architectural decision; present the trade-offs.
- Agent-generated working notes, scratch output, and transient planning artifacts may be created under `.agent_scratchpad/` (git-ignored). This is the only location at repository root where agents may create files without explicit per-file approval.
- Do not introduce stubs or workarounds without explicit approval. Fix the root cause.
- **Absolute secrets ban (non-overridable):** Under no circumstances may any token, password, API key, connection string, certificate private key, or other secret/credential value exist anywhere inside the repository working tree — tracked or untracked, committed or gitignored, including `.env`, `*.env`, `*.pem`, MCP `envFile`s, scratch pads, fixtures, docs, configs, scripts, and agent-generated files. Gitignore is not permission to store secrets on disk under the repo. Process environment variables, OS keychain/secret stores, Azure Key Vault, GitHub Actions secrets, and .NET user secrets outside the tree are the only allowed locations. Convenience, MCP setup, broken tooling, user urgency, role instructions, skills, scoped `AGENTS.md`, and “make it work” workarounds cannot override this rule. Prefer a broken local tool over writing a secret into the tree. If a secret is discovered in the tree, stop, remove it, rotate the credential, and do not continue the original task until the working tree is clean of secrets. Redact secrets and PII from logs and prompts.
- Azure CLI reads are not permitted because this repository does not document an approved subscription allowlist. Azure writes, local `terraform apply` / `terraform destroy`, direct Docker builds, and ACR pushes are forbidden. Terraform under `7-Deployment/` is the infrastructure-as-code for this repository; do not introduce Pulumi, Bicep, or Ansible.
- Fix issues within the task scope, including defects caused by the change. Record unrelated findings for follow-up through the approved todo workflow; do not expand the task scope without approval.
- Do not create new files or folders at repository root, with the single exception of `.agent_scratchpad/`. Scripts, tools, and deployment assets belong under `7-Deployment/`; governed documentation belongs under `6-Docs/`; generated notes belong in the scratchpad. `6-Docs/` holds canonical documentation only — never scratch output, vendored packages, or git-ignored working files.

Read the [security directives](6-Docs/security.md) when a task touches security. Azure CLI reads remain prohibited until an approved subscription allowlist is documented.

## Local verification environment

DB-backed tests (`HotshotLogistics.IntegrationTests`, the repository and controller classes in `HotshotLogistics.Tests`) need SQL Server. Run `7-Deployment/dev-env/dev-env-up.sh` first: it starts Docker, brings up the container, creates the database and applies migrations (about 20 s cold, 7 s when already up). `7-Deployment/dev-env/dev-env-down.sh` stops it (`--purge` also deletes the data). Details: [DbSetup onboarding](6-Docs/dbsetup/onboarding.md). The dashboard builds in a fresh worktree with `npm ci && npm run build`; `1-Presentation/admin-dashboard/.env.production` holds placeholder IDs only.

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

<!-- KYBER_WEAVE_CONFIG_REG_START -->
## Repository Configuration & Paths Registry (Config Reg)

Agents and skills look up the following properties to find the documentation and
references that belong to this repository.

- **<docs-root>**: `6-Docs`
- **<documentation-index>**: `6-Docs/README.md`
- **<documentation-ontology>**: `6-Docs/documentation-ontology.md`
- **<component-catalog>**: `6-Docs/catalog.md`
- **<standards-root>**: `6-Docs/standards`
- **<azure-coding-standard>**: `6-Docs/standards/azure/README.md`
- **<csharp-coding-standard>**: `6-Docs/standards/csharp/README.md`
- **<data-access-layer-coding-standard>**: `6-Docs/standards/data-access-layer/README.md`
- **<github-actions-coding-standard>**: `6-Docs/standards/github-actions/README.md`
- **<react-coding-standard>**: `6-Docs/standards/react/README.md`
- **<sql-coding-standard>**: `6-Docs/standards/sql/README.md`
- **<test-coding-standard>**: `6-Docs/standards/test/README.md`
- **<plan-index>**: `6-Docs/plans/README.md`
- **<specification-index>**: `6-Docs/specs/README.md`
- **<todo-index>**: `6-Docs/todo/README.md`
- **<adr-index>**: `6-Docs/adr/README.md`
- **<rules-index>**: `6-Docs/rules/README.md`
- **<reference-index>**: `6-Docs/reference/README.md`
- **<onboarding>**: `done`

Skills and other portable instruction files SHALL reference these paths by the
property name above — "the path declared as **<component-catalog>**
in the repository root `AGENTS.md`" — rather than embedding a relative link that
traverses out of the skill's own directory. A skill written that way stays correct
when this repository moves something; only this registry has to change.

Generated by `kyber-weave docs init` from `.kyber-weave/kyber-weave.yml`. Edit that
file rather than this block: everything between the markers is rewritten on the next
run.
<!-- KYBER_WEAVE_CONFIG_REG_END -->
