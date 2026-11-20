---
id: security-guidelines
title: Security guidelines
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-08-14
---

# Security Guidelines

Never check secrets of any kind into source control. Never store secrets in plain text anywhere. GitHub's policy file is [SECURITY.md](../SECURITY.md).

## Secure Values

Values that must never be stored in plain text include:

- Connection strings
- Passwords
- PAT tokens
- Bearer tokens
- Tokens of any kind
- API keys
- Client secrets

## Security Best Practices

1. Use Azure AD authentication with role-based access control
2. Validate all user inputs on client and server
3. Configure CORS for production
4. Use HTTPS for API traffic
5. Least privilege for service accounts
6. Keep dependencies updated
7. Validate JWT tokens on protected endpoints
8. Store production secrets in Azure Key Vault; local API configuration uses .NET user secrets; dashboard public Azure AD ids come from the process environment. Do not write secrets into the repository tree, including `.env` files.
9. Enforce HTTP Strict Transport Security (HSTS) for communications.
10. Implement authentication and authorization controls.
11. Protect against Cross-Site Request Forgery (CSRF) attacks where relevant.
12. Persist and regularly rotate Data Protection API keys used for encryption.
13. Use layered security controls appropriate to the service, including rate limiting where required; see the [C# coding standard](standards/csharp/csharp.standards.md#http-pipeline) for implementation guidance.

## Secret Management

- Azure Key Vault and App Configuration for Azure
- .NET user secrets for local API development
- Process environment for dashboard `NEXT_PUBLIC_*` values (generated `.env.local` must stay gitignored)
- Placeholders only in example configuration
- Rotate secrets when they leak
