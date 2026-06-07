# Implementation Plan: Expense Tracking MVP (Web App + APIs)

**Branch**: `001-expense-tracking-mvp` | **Date**: 2026-06-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-expense-tracking-mvp/spec.md`

## Summary

Build an end-to-end personal expense tracker comprising a **.NET 10 ASP.NET Core Web API** and an **Angular 21** SPA. Users register (phone, email, password, first/last name, optional photo, selected currency), log in by email or phone, and manage daily Income/Expense entries inside a Monthly View with totals and savings. A carry-forward chain projects opening balances across months; a dashboard renders an expense graph and a color-banded savings-rate indicator. Admins manage all users, the global Category catalog (Expense/Income), and the supported-Currency catalog. Future months are read-only with a projected opening balance. Authentication is JWT bearer; authorization is **policy-based** (role + resource ownership). Persistence is **EF Core 10 with SQLite by default** and pluggable to other relational providers via configuration.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript 5.x with Angular 21 (frontend)

**Primary Dependencies**:
- Backend: ASP.NET Core 10, Entity Framework Core 10 (SQLite + Relational), `Microsoft.AspNetCore.Authentication.JwtBearer`, ASP.NET Core RateLimiter, Serilog (or built-in `ILogger` + JSON formatter), Swashbuckle/Swagger for OpenAPI, FluentValidation, BCrypt.Net-Next (or Argon2 via `Konscious.Security.Cryptography`), xUnit + WebApplicationFactory.
- Frontend: Angular 21 (standalone components, modern control flow), Angular Router, Angular Forms (Reactive), Angular HttpClient, RxJS, ng2-charts + Chart.js (or `@swimlane/ngx-charts`) for the dashboard graph, ESLint + Prettier, Jest (preferred) or Karma+Jasmine, Playwright for e2e.

**Storage**: SQLite (default, dev); pluggable EF Core relational providers (e.g., SQL Server, PostgreSQL, MySQL) selected at startup via configuration (Factory pattern). Profile photos stored as bytes in DB for MVP (justified in research.md).

**Testing**: xUnit unit tests (Domain + Application); xUnit integration tests using `WebApplicationFactory<Program>` against an in-memory SQLite database; Jest unit tests for Angular; Playwright e2e for critical flows (register → record entry → see totals).

**Target Platform**: Web (modern evergreen browsers). API hosted as a Linux container; SPA served as static files (CDN or embedded in API host). Single-deployment topology in MVP.

**Project Type**: Web application (frontend SPA + backend API).

**Performance Goals**: Monthly View loads in **< 1.5 s** p95 with 12 months × 200 entries (SC-008). Add/edit/delete reflects in totals **< 1 s** (SC-002). Carry-forward propagation across all later months **< 2 s** (SC-003). API target p95 **< 200 ms** for read endpoints under 50 RPS per user.

**Constraints**:
- HTTPS enforced outside Development.
- All endpoints authenticated by default; only `POST /auth/register`, `POST /auth/login`, `GET /health` are anonymous.
- No FX conversion on currency change.
- Future-month writes rejected by API; future-month reads return projected opening balance with empty entries.
- Carry-forward recompute is synchronous and transactional; partial updates not allowed.

**Scale/Scope**: MVP target ≤ 10,000 registered users, ≤ 24 months of history per user, ≤ 200 entries per user-month. ~20 API endpoints, ~15 Angular feature components.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Evidence |
|---|---|---|
| **I. Clean Layered Architecture (NON-NEGOTIABLE)** | PASS | Backend split into 4 projects: `ExpenseTracker.Domain`, `ExpenseTracker.Application`, `ExpenseTracker.Infrastructure`, `ExpenseTracker.Api`. Inner→outer dependencies enforced by project references. Angular split into `domain/`, `application/`, `infrastructure/`, `presentation/` folders with ESLint dependency-direction rule. |
| **II. Pluggable Persistence & Provider Abstraction** | PASS | `Database:Provider` config key + `IDbContextOptionsFactory` (Factory) selects SQLite/SqlServer/PostgreSQL/MySQL at startup. All access goes through `IRepository<T>` and aggregate-specific repositories defined in Domain. EF Core migrations checked in per provider. |
| **III. Security-First Middleware Pipeline** | PASS | Pipeline order: Exception handler (ProblemDetails) → HTTPS redirection (non-Dev) → CORS (config allow-list) → Rate limiter (per-IP + per-user policies) → Auth (JWT) → Authorization (policy-based: `RequireOwnership`, `RequireAdmin`). Secrets via env / user-secrets. |
| **IV. Pattern-Driven, Documented Code** | PASS | Patterns: Repository (per aggregate), Factory (DB provider, JWT signer), Adapter (photo storage, password hasher), Strategy (policy handlers), Options (Jwt, Cors, RateLimit, Database). XML doc comments on Domain/Application; TSDoc on Angular domain/application; per-project README; OpenAPI/Swagger for endpoints with examples. |
| **V. Coding Standards & Quality Gates** | PASS | `.editorconfig`, nullable refs enabled solution-wide, `TreatWarningsAsErrors=true` in Release, Roslyn + StyleCop analyzers; `dotnet format` in CI. Angular `strict: true`, ESLint + Prettier; CI fails on warnings. |
| **Tech Stack & Standards** | PASS | .NET 10, EF Core 10, SQLite default, JWT, Swagger, xUnit; Angular 21 standalone + signals, Jest, Playwright. |
| **Workflow & Quality Gates** | PASS | Feature branch `001-expense-tracking-mvp`; PR gate (build + lint + analyzers + tests + OpenAPI emit). Migrations ship with schema changes. Structured logging w/ correlation IDs via Serilog. |

**Result**: All gates pass. No deviations. The Complexity Tracking table at the end of this file is empty.

**Re-check after Phase 1 design**: PASS — design artifacts (data-model, contracts, quickstart) introduce no deviations; layering and pluggability preserved; new artifacts strengthen, not weaken, gates.

## Project Structure

### Documentation (this feature)

```text
specs/001-expense-tracking-mvp/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── openapi.yaml     # REST API contract
│   └── README.md        # contract conventions
├── checklists/
│   └── requirements.md  # spec quality checklist (already exists)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── ExpenseTracker.sln
├── src/
│   ├── ExpenseTracker.Domain/                  # entities, value objects, abstractions (no deps)
│   │   ├── Entities/                           # User, Role, Currency, Category, Entry, MonthlySummary
│   │   ├── ValueObjects/                       # Money, EmailAddress, PhoneNumber, MonthYear
│   │   ├── Abstractions/                       # IUserRepository, IEntryRepository, ICategoryRepository, ICurrencyRepository, IUnitOfWork, IPasswordHasher, IClock, ITokenService, IPhotoStorage
│   │   └── Services/                           # CarryForwardCalculator, MonthlySummaryService, SavingsRateClassifier
│   ├── ExpenseTracker.Application/             # use cases, DTOs, validators (depends on Domain)
│   │   ├── Auth/                               # RegisterUser, LoginUser
│   │   ├── Entries/                            # CreateEntry, UpdateEntry, DeleteEntry, ListEntriesByMonth
│   │   ├── Months/                             # GetMonthlyView, GetDashboard
│   │   ├── Admin/                              # ListUsers, UpdateUser, DeleteUser, ManageCategory, ManageCurrency
│   │   ├── DTOs/
│   │   ├── Validators/                         # FluentValidation
│   │   └── Common/                             # Result<T>, AppException, AuthorizationPolicy names
│   ├── ExpenseTracker.Infrastructure/          # EF Core, adapters (depends on Application + Domain)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/                 # IEntityTypeConfiguration<T>
│   │   │   ├── Repositories/
│   │   │   ├── DbContextOptionsFactory.cs      # Factory: provider selection
│   │   │   └── Migrations/
│   │   │       ├── Sqlite/
│   │   │       └── SqlServer/                  # added when needed
│   │   ├── Identity/                           # PasswordHasher, JwtTokenService
│   │   ├── Storage/                            # DatabasePhotoStorage adapter
│   │   └── Time/                               # SystemClock
│   └── ExpenseTracker.Api/                     # ASP.NET Core host (depends on Infrastructure + Application)
│       ├── Program.cs                          # composition root
│       ├── Endpoints/ or Controllers/          # AuthController, EntriesController, MonthsController, AdminController, CategoriesController, CurrenciesController, DashboardController, HealthController
│       ├── Middleware/                         # ExceptionHandlerMiddleware
│       ├── Authorization/                      # Policy names + handlers (RequireOwnership, RequireAdmin)
│       ├── Options/                            # JwtOptions, CorsOptions, RateLimitOptions, DatabaseOptions
│       └── appsettings.json / .Development.json
└── tests/
    ├── ExpenseTracker.Domain.Tests/            # xUnit unit
    ├── ExpenseTracker.Application.Tests/       # xUnit unit
    └── ExpenseTracker.Api.IntegrationTests/    # WebApplicationFactory + SQLite in-memory

frontend/
├── package.json
├── angular.json
├── eslint.config.mjs
├── src/
│   ├── app/
│   │   ├── domain/                             # entities, interfaces, use-case contracts (no Angular deps)
│   │   │   ├── models/                         # User, Entry, Category, Currency, MonthlySummary
│   │   │   ├── value-objects/                  # Money, MonthYear
│   │   │   └── ports/                          # AuthRepository, EntriesRepository, MonthsRepository, AdminRepository
│   │   ├── application/                        # use cases / interactors
│   │   │   ├── auth/
│   │   │   ├── entries/
│   │   │   ├── months/
│   │   │   ├── dashboard/
│   │   │   └── admin/
│   │   ├── infrastructure/                     # adapters
│   │   │   ├── http/                           # AuthHttpRepository, EntriesHttpRepository, ...
│   │   │   ├── auth/                           # JwtTokenStore, AuthInterceptor
│   │   │   └── config/                         # ApiConfig
│   │   ├── presentation/                       # Angular components, routing, state
│   │   │   ├── features/
│   │   │   │   ├── auth/ (register, login)
│   │   │   │   ├── monthly-view/
│   │   │   │   ├── entry-form/
│   │   │   │   ├── dashboard/
│   │   │   │   └── admin/ (users, categories, currencies)
│   │   │   ├── shared/                         # ui components, pipes, directives
│   │   │   ├── core/                           # guards (AuthGuard, AdminGuard), interceptors
│   │   │   └── app.routes.ts
│   │   └── app.config.ts
│   └── styles/
└── tests/
    ├── unit/                                   # Jest
    └── e2e/                                    # Playwright
```

**Structure Decision**: **Web application** layout (Option 2 from the template) with two top-level folders, `backend/` and `frontend/`, each implementing Clean Architecture per the constitution's Principle I. The backend is a 4-project .NET solution; the frontend is a single Angular workspace organized into `domain/`, `application/`, `infrastructure/`, `presentation/` folders with an ESLint dependency-direction rule preventing inner-from-outer imports.

## Complexity Tracking

> No constitution violations to justify. Section intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_ | — | — |
