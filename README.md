# ExpenseTracker

A two-tier expense tracker with a .NET 10 / ASP.NET Core API backend and an Angular 21+ Single Page Application frontend, organized as Clean Architecture on both sides.

## What it does

- Users register, log in, and track monthly **income** and **expenses**.
- The API computes **monthly summaries**, **carry-forward balances**, **YTD totals**, and a **savings-rate status color** (Green / Orange / OrangeRedTint / BloodRed).
- Admins manage users, categories, and currencies.

## Repository layout

```
backend/    .NET 10 API + Domain / Application / Infrastructure / Api
frontend/   Angular 21+ SPA, standalone components, Signals, Reactive Forms
specs/      GitHub Spec-Kit artifacts (specification, plan, tasks)
.specify/   Spec-Kit governance (constitution, scripts, templates)
```

## Getting started

Read the documents in this order:

1. [.specify/memory/constitution.md](.specify/memory/constitution.md) — non-negotiables.
2. [specs/001-expense-tracking-mvp/spec.md](specs/001-expense-tracking-mvp/spec.md) — feature specification.
3. [specs/001-expense-tracking-mvp/plan.md](specs/001-expense-tracking-mvp/plan.md) — tech stack & structure.
4. [specs/001-expense-tracking-mvp/quickstart.md](specs/001-expense-tracking-mvp/quickstart.md) — local run & acceptance walkthrough.

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/ExpenseTracker.Api
```

The API listens on `http://localhost:5266` by default. SQLite database is created on first run and a default admin is seeded from `Seed:DefaultAdmin*` settings.

### Frontend

```bash
cd frontend
npm install
npm start
```

The dev server runs on `http://localhost:4200` and proxies API calls to the backend's base URL configured in [frontend/src/app/infrastructure/config/api.config.ts](frontend/src/app/infrastructure/config/api.config.ts).

### Tests

```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

## Architecture overview

- **Backend Clean Architecture**: `Domain → Application → Infrastructure → Api`. Inner layers never depend on outer layers.
- **Frontend Clean Architecture**: `domain → application → infrastructure → presentation`. Presentation imports application; application imports domain only; infrastructure adapters implement domain ports.
- **Repository + Unit-of-Work** for persistence; **per-User SemaphoreSlim coordinator** for write serialization; **synchronous transactional carry-forward** recomputation.
- **JWT bearer + policy-based authorization** (`RequireUser`, `RequireAdmin`, resource-based `EntryOwner`).
- **RFC 7807 ProblemDetails** with a `code` extension on every error.

See [specs/001-expense-tracking-mvp/plan.md](specs/001-expense-tracking-mvp/plan.md) for the full rationale.
