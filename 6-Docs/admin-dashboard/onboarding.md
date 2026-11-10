---
id: admin-dashboard/onboarding
title: Admin dashboard onboarding
doc-type: onboarding
status: current
component: admin-dashboard
source-root: 1-Presentation/admin-dashboard
owner: unassigned
last-reviewed: 2026-11-10
---

# Admin dashboard onboarding

## Dependencies

- Node.js 20.9+ (the minimum the installed Next.js 16.4.6 declares in its `engines` field)
- npm (lockfile is `package-lock.json`)
- A running API (see [API onboarding](../api/onboarding.md))

## Setup

1. `cd 1-Presentation/admin-dashboard`
2. `npm install`
3. Set Azure AD public identifiers in the **process environment** (not files committed to the repo):

```bash
export NEXT_PUBLIC_AZURE_CLIENT_ID="<app-registration-client-id>"
export NEXT_PUBLIC_AZURE_TENANT_ID="<tenant-id>"
```

`npm run dev` and `npm run build` run `scripts/generate-env.mjs` (the `predev` and
`prebuild` scripts), which writes a local `.env.local` from those variables. That
generated file must stay gitignored and must never be added to the tree.

4. `npm run dev` — Next.js with `--experimental-https` and the locally generated
`certificates/localhost.pem` / `certificates/localhost-key.pem`, which are not in the
tree. Open the URL printed by the dev server (typically https://localhost:3000).

## Debug path

- Auth is MSAL (`@azure/msal-browser` / `@azure/msal-react`).
- API calls go through `src/services/api.ts`.
- Real-time updates use `@microsoft/signalr`.
- Playwright: `npm run test:install` once (installs browsers), then `npm test`
  (`playwright test`). There is no `pretest` hook.

## Non-standard operating procedures

- Do not commit `.env`, `.env.local`, or Azure AD secrets.
- The dashboard is not hosted on Vercel in this repository; Azure deployment is Terraform under `7-Deployment/`.
