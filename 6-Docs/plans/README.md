---
id: plans/index
title: Plans
doc-type: index
status: draft
owner: 'unassigned'
last-reviewed: 2026-10-20
---

# Plans

Sequenced implementation work: what will be done, in what order, and how it will be
verified. A plan is a record of intent rather than current guidance — retrieval
demotes it accordingly, and it is archived once closed.

**No active plan remains.** Every plan below has been delivered, verified against the
implemented behavior and its canonical documentation, and archived. `Archived` plans are
historical records, not implementation authority; open a plan only when it is listed here
as `Draft`, `Ready`, `In progress`, or `Blocked`.

| Plan | Status | Goal | Implementation reference | Archive date |
|---|---|---|---|---|
| [Deployment and CLI architecture documentation](../archive/plans/devops-deployment-architecture-documentation.md) | Archived | Author `6-Docs/devops/` deployment + DbSetup CLI documentation; reconcile `6-Docs/dbsetup/`; record known drift | [`6-Docs/devops/README.md`](../devops/README.md) · [`deployment-architecture.md`](../devops/deployment-architecture.md) (drift register DR1–DR6) · [`ci-cd-pipelines.md`](../devops/ci-cd-pipelines.md) · [`terraform-modules.md`](../devops/terraform-modules.md) · [`6-Docs/dbsetup/architecture.md`](../dbsetup/architecture.md) · [`6-Docs/dbsetup/onboarding.md`](../dbsetup/onboarding.md) · [`6-Docs/technology-stack.md`](../technology-stack.md) · [`6-Docs/README.md`](../README.md) | 2026-10-20 |
| [Deployment drift register and residual-claim remediation](../archive/plans/devops-drift-remediation.md) | Archived | Amend the canonical drift register (DR1–DR2 file lists + DR7–DR15) and correct residual false claims across `6-Docs/` and component READMEs | [`6-Docs/devops/deployment-architecture.md`](../devops/deployment-architecture.md) (canonical drift register DR1–DR15) · [`ci-cd-pipelines.md`](../devops/ci-cd-pipelines.md) · [`terraform-modules.md`](../devops/terraform-modules.md) · [`6-Docs/devops/README.md`](../devops/README.md) · [`6-Docs/technology-stack.md`](../technology-stack.md) · [`6-Docs/catalog.md`](../catalog.md) · [`6-Docs/api/architecture.md`](../api/architecture.md) · [`6-Docs/api/onboarding.md`](../api/onboarding.md) · [`6-Docs/project-overview.md`](../project-overview.md) · [`6-Docs/rules/scripts.md`](../rules/scripts.md) · [`6-Docs/rules/file-organization.md`](../rules/file-organization.md) · [`7-Deployment/DbSetup/README.md`](../../7-Deployment/DbSetup/README.md) · [`1-Presentation/HotshotLogistics.Api/README.md`](../../1-Presentation/HotshotLogistics.Api/README.md) · root [`README.md`](../../README.md) | 2026-10-20 |

## Where the delivered work and its decisions are recorded

- **The canonical drift register — DR1–DR15, the single source of truth for deployment
  drift — lives in
  [`6-Docs/devops/deployment-architecture.md`](../devops/deployment-architecture.md#canonical-drift-register).
  The superseded 2026-10-19 entries are retained verbatim in its
  [Register versioning](../devops/deployment-architecture.md#register-versioning)
  subsection, and the 2026-10-20 amendments are recorded in its
  [Register reconciliation note](../devops/deployment-architecture.md#register-reconciliation-note).
- **The deployment model, the CI/CD pipeline reference, and the Terraform module
  reference** live in
  [`6-Docs/devops/deployment-architecture.md`](../devops/deployment-architecture.md),
  [`6-Docs/devops/ci-cd-pipelines.md`](../devops/ci-cd-pipelines.md), and
  [`6-Docs/devops/terraform-modules.md`](../devops/terraform-modules.md), indexed by
  [`6-Docs/devops/README.md`](../devops/README.md).
- **The DbSetup CLI documentation** lives in
  [`6-Docs/dbsetup/architecture.md`](../dbsetup/architecture.md) and
  [`6-Docs/dbsetup/onboarding.md`](../dbsetup/onboarding.md).
- **The catalog's ownership and review-date record** for the DbSetup component is the
  DbSetup row in [`6-Docs/catalog.md`](../catalog.md); the **documentation index** entry
  for the new `devops/` folder is the `devops/` row in [`6-Docs/README.md`](../README.md).
- **The archived plans themselves** carry their permanent `## Decisions` tables — every
  row and its approval provenance, including the approve-and-execute approvals (D9 on
  2026-10-19 and D10 on 2026-10-20). Archived material is never current guidance; read
  the canonical documents above for what the system does today.
