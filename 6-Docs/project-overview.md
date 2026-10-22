---
id: project-overview
title: Hotshot Logistics project overview
doc-type: reference
status: current
owner: unassigned
last-reviewed: 2026-10-20
---

# Hotshot Logistics Project Overview

Hotshot Logistics is a full-stack platform for hotshot delivery operations: coordinating jobs, drivers, customers, tracking, and billing. Cataloged runnables in the tree today are the admin dashboard, the ASP.NET Core API, and the DbSetup CLI. There is no driver mobile app in this repository.

## System Components

- **Admin Dashboard**: Next.js UI for jobs, drivers, customers, tracking, and billing. See [admin-dashboard architecture](admin-dashboard/architecture.md).
- **Backend API**: ASP.NET Core Web API (hosting model unresolved — see DR2), SQL Server via native ADO.NET, Clean Architecture. See [API architecture](api/architecture.md).
- **DbSetup**: Console tool that provisions SQL Server databases and application accounts. See [DbSetup architecture](dbsetup/architecture.md).

## Project Context

The platform replaces paper-based job assignment and manual tracking with a digital dispatch surface and a cloud API. Motivations:

- Eliminate paper-based job assignments and manual tracking
- Provide real-time visibility into delivery operations
- Enable data-driven decisions through the admin UI
- Scale on Azure without abandoning Clean Architecture

A driver mobile client is out of scope until source exists under `1-Presentation/`.
