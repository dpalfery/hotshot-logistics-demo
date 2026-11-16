---
id: admin-dashboard/onboarding
title: Admin dashboard onboarding
doc-type: onboarding
status: current
component: admin-dashboard
source-root: 1-Presentation/admin-dashboard
owner: unassigned
last-reviewed: 2026-11-16
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

### Local live status-count verification

For browser-to-database verification, use a development database configured via
the API's user secrets, without printing its connection string. Check that the
ASP.NET Core development certificate is already trusted with
`dotnet dev-certs https --check --trust` and that ports 5001 and 3000 are free.
Run the API from the repository root with
`ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=https://localhost:5001 dotnet run --project 1-Presentation/HotshotLogistics.Api --no-launch-profile`.
Run the dashboard from its source directory with
`NEXT_PUBLIC_API_BASE_URL=https://localhost:5001/api npx next dev --port 3000`
and the public identifiers from Setup in the environment. This HTTP dashboard
origin and HTTPS API origin are permitted by development CORS and CSP.

Use a browser with normal TLS validation to compare the displayed cards with the
unscoped API response. A uniquely owned customer and Pending job can verify a
one-count increase and the customer-scoped response; delete both afterwards and
confirm scoped counts are zero. Do not bypass certificate validation or change
security configuration to make the check pass.

## Non-standard operating procedures

- Do not commit `.env`, `.env.local`, or Azure AD secrets.
- The dashboard is not hosted on Vercel in this repository; Azure deployment is Terraform under `7-Deployment/`.
