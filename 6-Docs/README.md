---
id: documentation-index
title: Documentation
doc-type: index
status: draft
owner: 'unassigned'
last-reviewed: 2026-10-19
---

# Documentation

The governed documentation corpus for this repository. Every document under
`6-Docs/` conforms to [the documentation ontology](documentation-ontology.md) and is
checked by `kyber-weave docs validate` and `kyber-weave docs drift`.

| Directory | Holds |
|---|---|
| [standards/](standards/README.md) | Coding standards, one per technology |
| [plans/](plans/README.md) | Sequenced implementation work |
| [specs/](specs/README.md) | Upfront specification work |
| [todo/](todo/README.md) | Work identified but not done now |
| [adr/](adr/README.md) | Architecture decision records |
| [rules/](rules/README.md) | Repository-wide rules |
| [devops/](devops/README.md) | Deployment architecture, CI/CD pipelines, and Terraform infrastructure reference, plus the canonical drift register |
| [reference/](reference/README.md) | Reference material |

[`catalog.md`](catalog.md) is the authoritative vocabulary for the `component` and
`owner` keys. Start there when adding a document for something new.
