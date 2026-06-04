<!-- SPECKIT START -->
# ExpenseTracker — Active Feature Context

**Active feature**: `001-expense-tracking-mvp`
**Branch**: `001-expense-tracking-mvp`

For technologies, architecture, project structure, conventions, and shell
commands relevant to this feature, read these files in order:

- Constitution (governance + non-negotiables): `.specify/memory/constitution.md`
- Implementation plan (tech stack + structure decision):
  `specs/001-expense-tracking-mvp/plan.md`
- Feature specification (user stories + functional requirements):
  `specs/001-expense-tracking-mvp/spec.md`
- Phase 0 research (decisions and rationale):
  `specs/001-expense-tracking-mvp/research.md`
- Data model: `specs/001-expense-tracking-mvp/data-model.md`
- API contracts: `specs/001-expense-tracking-mvp/contracts/openapi.yaml`
- Quickstart (local run + acceptance walkthrough):
  `specs/001-expense-tracking-mvp/quickstart.md`

## Stack snapshot

- Backend: .NET 10, ASP.NET Core, EF Core 10 (SQLite default; pluggable),
  JWT bearer + policy-based authorization, Serilog, xUnit + WebApplicationFactory.
- Frontend: Angular 21 (standalone components, modern control flow, Signals),
  ng2-charts, ESLint + Prettier, Jest, Playwright.
- Layout: Clean Architecture on both sides
  (`Domain → Application → Infrastructure → Api/Presentation`).

## Reminders

- Inner layers never depend on outer layers — verify imports/project references.
- Repository pattern for all persistence; DB provider chosen via the
  `DbContextOptionsFactory` from `Database:Provider` config.
- All endpoints require auth by default; only register/login/refresh,
  `GET /currencies/active`, and `GET /health` are anonymous.
- Reject writes targeting a future month (`code=future_month_write_forbidden`).
- Carry-forward is recomputed synchronously inside the same transaction.
- Coding gates: nullable refs on, warnings-as-errors in Release, `dotnet format`
  clean, Angular `strict: true`, no lint warnings in CI.
<!-- SPECKIT END -->
