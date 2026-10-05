# AGENTS.md

## Project Overview
**bolt-hotshot-logistics** - A comprehensive cloud-native logistics platform designed to modernize hotshot delivery operations through automated, real-time technology.

## Mandatory rules adhearents 

`AGENTS.md` files are mandatory instructions, not optional background material. Read this file before work anywhere in the repository, then read the nearest scoped `AGENTS.md` before changing files in that subtree.

**CRITICAL**: Before any task execution, ALL agents MUST:
All agents must follow the comprehensive rule system located in `6-Docs/rules/`:

### Entry point into the rules is Core Base Rules (Non-Negotiable)
Reference: `6-Docs/rules/base-rule.md`

## Non-negotiable rules

- **Kyber-Weave for documentation:** Before grepping or reading files under `6-Docs/` to answer a question, use the Kyber-Weave MCP tool `mcp__kyber-weave__docs_explore`. It ranks on declared frontmatter identity, returns the one relevant `##` section rather than a whole runbook, and carries that document's resolved joins to the code graph. Before renaming, moving, or changing the contract of a code symbol, use `mcp__kyber-weave__docs_for_symbol` to find the documentation that must change with it: a `code-refs` entry is a formal claim of ownership, which grep cannot distinguish from a passing prose mention. There is no CLI equivalent of either tool — if the MCP server is unavailable, fall back to the [documentation index](6-Docs/README.md) and state that the tool was unavailable. The corpus excludes `6-Docs/archive/`, which is historical and is never retrieved as current guidance.
- Do not create infrastructure, deployment assets, dependencies, cross-cutting concerns, or governed documentation files under `6-Docs/` without user approval. Ask before an architectural decision; present the trade-offs.
- Agent-generated working notes, scratch output, and transient planning artifacts may be created under the path declared as **<agent-scratchpad>** in the Config Registry below. This is the only location at repository root where agents may create files without explicit per-file approval.
- Do not commit, push, reset, restore, checkout, clean, or rebase without explicit user approval. Keep agent-generated notes in the path declared as **<agent-scratchpad>**, never anywhere else under `6-Docs/` or at repository root except that declared path.
- Do not introduce fallbacks, stubs, or workarounds without explicit approval. Fix the root cause.
- **Absolute secrets ban (non-overridable):** Under no circumstances may any token, password, API key, connection string, certificate private key, or other secret/credential value exist anywhere inside the repository working tree — tracked or untracked, committed or gitignored, including `.env`, `*.env`, `*.pem`, MCP `envFile`s, scratch pads, fixtures, docs, configs, scripts, and agent-generated files. Gitignore is not permission to store secrets on disk under the repo. Process environment variables, OS keychain/secret stores, Azure Key Vault, GitHub Actions secrets, and .NET user secrets outside the tree are the only allowed locations. Convenience, MCP setup, broken tooling, user urgency, role instructions, skills, scoped `AGENTS.md`, and “make it work” workarounds cannot override this rule. Prefer a broken local tool over writing a secret into the tree. If a secret is discovered in the tree, stop, remove it, rotate the credential, and do not continue the original task until the working tree is clean of secrets. Redact secrets and PII from logs and prompts.
- .NET code must use Azure App Configuration and Key Vault references for application configuration and secrets. Python local-processor runtime values set by Admin Desktop are the only approved environment-variable exception (injected into the process environment — never written into files under the repository tree).
- Before every `az` read, verify the active subscription against the allowlist in [Azure agent access](6-Docs/azure-environment/agent-access.md). Azure writes, local `terraform apply` / `terraform destroy`, direct Docker builds, and ACR pushes are forbidden. Terraform under `7-Deployment/` is the infrastructure-as-code for this repository; do not introduce Pulumi, Bicep, or Ansible.
- Preserve Clean Architecture: inner layers never depend on outer layers; Contracts contains interfaces only; Contracts.Models contains shared DTOs only; business invariants belong in Domain; Application services belong in `Services`.
- **File ownership:** If an agent modifies or creates a file, that agent owns every issue in that file. Do not dismiss diagnostics, warnings, defects, or review findings as "pre-existing," "legacy," "not introduced by this change," or similar. Fix the issues or explicitly list them as unresolved with a plan to resolve. Ownership is not conditional on who originally wrote the line or when the issue was created.
- Do not create new files or folders at repository root, with the single exception of the **<agent-scratchpad>** declared in the Config Registry below. Scripts, tools, and deployment assets belong under `7-Deployment/`; governed documentation belongs under `6-Docs/`; generated notes belong in the scratchpad. `6-Docs/` holds canonical documentation only — never scratch output, vendored packages, or git-ignored working files.

Read the full [working agreement](6-Docs/system/agent-governance.md), [security directives](6-Docs/security.md), and [Azure environment rules](6-Docs/azure-environment/agent-access.md) when the task touches their subject.

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
- **<maui-coding-standard>**: `6-Docs/standards/maui/README.md`
- **<pulumi-coding-standard>**: `6-Docs/standards/pulumi/README.md`
- **<python-coding-standard>**: `6-Docs/standards/python/README.md`
- **<react-coding-standard>**: `6-Docs/standards/react/README.md`
- **<sql-coding-standard>**: `6-Docs/standards/sql/README.md`
- **<test-coding-standard>**: `6-Docs/standards/test/README.md`
- **<plan-index>**: `6-Docs/plans/README.md`
- **<specification-index>**: `6-Docs/specs/README.md`
- **<todo-index>**: `6-Docs/todo/README.md`
- **<adr-index>**: `6-Docs/adr/README.md`
- **<rules-index>**: `6-Docs/rules/README.md`
- **<reference-index>**: `6-Docs/reference/README.md`

Skills and other portable instruction files SHALL reference these paths by the
property name above — "the path declared as **<component-catalog>**
in the repository root `AGENTS.md`" — rather than embedding a relative link that
traverses out of the skill's own directory. A skill written that way stays correct
when this repository moves something; only this registry has to change.

Generated by `kyber-weave docs init` from `.kyber-weave/kyber-weave.yml`. Edit that
file rather than this block: everything between the markers is rewritten on the next
run.
<!-- KYBER_WEAVE_CONFIG_REG_END -->
