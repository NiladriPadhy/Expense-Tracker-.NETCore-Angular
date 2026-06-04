---
description: "Task list for Expense Tracking MVP (feature 001-expense-tracking-mvp)"
---

# Tasks: Expense Tracking MVP

**Input**: Design documents from `/specs/001-expense-tracking-mvp/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/openapi.yaml`

**Tests**: Included. The Constitution (Principle V) requires unit tests for public contracts and recommends integration tests; research R-012 specifies xUnit + WebApplicationFactory, Jest, and Playwright. Test tasks are therefore part of every user-story phase.

**Organization**: Tasks are grouped by user story. Each user-story phase is an independently testable, independently shippable slice. US1 alone constitutes the MVP-of-the-MVP (registration + login + reach Monthly View shell).

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User-story label (e.g., [US1])
- Exact file paths are included.

## Path Conventions

Web app layout per `plan.md` Structure Decision:

- Backend: `backend/src/ExpenseTracker.{Domain,Application,Infrastructure,Api}/`, tests under `backend/tests/`.
- Frontend: `frontend/src/app/{domain,application,infrastructure,presentation}/`, tests under `frontend/tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository scaffolding, tool baselines, CI gates. No user-story logic.

- [ ] T001 Create backend solution and project skeletons: `backend/ExpenseTracker.sln`, `backend/src/ExpenseTracker.Domain/ExpenseTracker.Domain.csproj`, `backend/src/ExpenseTracker.Application/ExpenseTracker.Application.csproj`, `backend/src/ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj`, `backend/src/ExpenseTracker.Api/ExpenseTracker.Api.csproj`. Wire project references Domain ← Application ← Infrastructure ← Api per plan.md Structure Decision.
- [ ] T002 [P] Create test project skeletons: `backend/tests/ExpenseTracker.Domain.Tests/`, `backend/tests/ExpenseTracker.Application.Tests/`, `backend/tests/ExpenseTracker.Api.IntegrationTests/` (xUnit) with references to the matching production projects.
- [ ] T003 [P] Add backend coding standards: `backend/.editorconfig`, `backend/Directory.Build.props` enabling `Nullable=enable` solution-wide, `TreatWarningsAsErrors=true` in Release, StyleCop analyzers, `LangVersion=latest`.
- [ ] T004 [P] Initialize Angular workspace at `frontend/` with `ng new expense-tracker --standalone --routing --style=scss --strict --skip-tests`; add Clean-Architecture folders `frontend/src/app/{domain,application,infrastructure,presentation/{features,shared,core}}` and root `frontend/src/app/app.routes.ts`, `frontend/src/app/app.config.ts`.
- [ ] T005 [P] Configure frontend tooling: install ESLint + Prettier + `eslint-plugin-boundaries` in `frontend/`; write `frontend/eslint.config.mjs` with a dependency-direction rule (presentation→infrastructure→application→domain only) and `frontend/.prettierrc`; add `npm run lint` and `npm run format` scripts to `frontend/package.json`.
- [ ] T006 [P] Configure frontend tests: install Jest + `@testing-library/angular`; add `frontend/jest.config.ts` and `frontend/tests/unit/` folder; install Playwright + `frontend/playwright.config.ts` and `frontend/tests/e2e/` folder; add `npm test` and `npm run e2e` scripts.
- [ ] T007 [P] Create `.github/workflows/ci.yml` with two jobs: **backend** (`dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build -c Release /p:TreatWarningsAsErrors=true`, `dotnet test`) and **frontend** (`npm ci`, `npm run lint`, `npm test -- --watch=false`, `ng build --configuration=production`). Both jobs block PR merge.
- [ ] T008 [P] Create top-level `README.md` linking the constitution (`.specify/memory/constitution.md`), the feature spec, the plan, and the quickstart. Resolves the constitution Sync Impact Report TODO.

**Checkpoint**: Solutions build empty; CI pipeline runs green; coding-standard files in place.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure required by every user story. **No user-story work may begin until this phase is complete.**

### Domain primitives (shared by all stories)

- [ ] T009 [P] Add `backend/src/ExpenseTracker.Domain/Common/Enums.cs` defining `EntryType { Expense, Income }`, `UserRole { User, Admin }`, `StatusColor { Green, Orange, OrangeRedTint, BloodRed }`.
- [ ] T010 [P] Add value objects in `backend/src/ExpenseTracker.Domain/ValueObjects/`: `Money.cs` (amount + currencyCode), `EmailAddress.cs`, `PhoneNumber.cs`, `MonthYear.cs` (year, month, comparisons, `Next()`, `Previous()`).
- [ ] T011 [P] Add `backend/src/ExpenseTracker.Domain/Abstractions/IClock.cs` and `backend/src/ExpenseTracker.Infrastructure/Time/SystemClock.cs` implementing `IClock` (UTC `Now`).
- [ ] T012 [P] Add `backend/src/ExpenseTracker.Application/Common/Result.cs` (Result<T> with error codes), `backend/src/ExpenseTracker.Application/Common/AppException.cs`, and `backend/src/ExpenseTracker.Application/Common/PolicyNames.cs` (`RequireUser`, `RequireAdmin`, `EntryOwner`).

### Persistence foundation (Principle II — pluggable provider)

- [ ] T013 Add `backend/src/ExpenseTracker.Infrastructure/Persistence/AppDbContext.cs` with empty `DbSet<>` placeholders and `OnModelCreating` applying configurations from assembly.
- [ ] T014 Add `backend/src/ExpenseTracker.Infrastructure/Persistence/DbContextOptionsFactory.cs` (Factory pattern) reading `Database:Provider` (`Sqlite|SqlServer|PostgreSQL|MySQL`) and `ConnectionStrings:Default`; register as singleton in `Api/Program.cs`. Default provider is `Sqlite`. Reference research.md R-001.
- [ ] T015 Add `backend/src/ExpenseTracker.Api/Options/DatabaseOptions.cs` bound from `Database` section; add to DI in `Program.cs`.
- [ ] T016 Generate initial SQLite migration scaffold under `backend/src/ExpenseTracker.Infrastructure/Persistence/Migrations/Sqlite/` (will be populated as entities are added).

### Composition root, middleware pipeline (Principle III — security-first)

- [ ] T017 Author `backend/src/ExpenseTracker.Api/Program.cs` composition root wiring DI, options, EF Core (via T014 factory), authentication, authorization, controllers/minimal APIs, Swagger, Serilog. Pipeline order: ExceptionHandler → HTTPS redirect (non-Dev) → CORS → RateLimiter → Authentication → Authorization → Endpoints.
- [ ] T018 [P] Add `backend/src/ExpenseTracker.Api/Middleware/ExceptionHandlerMiddleware.cs` mapping exceptions to RFC 7807 ProblemDetails with `code` extension; suppress stack traces outside Development.
- [ ] T019 [P] Add `backend/src/ExpenseTracker.Api/Options/CorsOptions.cs` (allow-list) and register CORS in `Program.cs` keyed off configuration.
- [ ] T020 [P] Add `backend/src/ExpenseTracker.Api/Options/RateLimitOptions.cs` and configure ASP.NET Core `RateLimiter` per research R-009 (global 100/min/IP, auth 5/min/IP sliding, authenticated 60/min/User token-bucket); return ProblemDetails on 429.
- [ ] T021 [P] Add `backend/src/ExpenseTracker.Api/Options/JwtOptions.cs` + JWT bearer authentication wiring in `Program.cs` (signing key from `Jwt:SigningKey` env/user-secret). Token validation parameters per research R-002.
- [ ] T022 Add policy-based authorization in `backend/src/ExpenseTracker.Api/Authorization/`: `EntryOwnerRequirement.cs`, `EntryOwnerAuthorizationHandler.cs`, and policy registration in `Program.cs` for `RequireUser`, `RequireAdmin`, `EntryOwner` (Admin bypass) per research R-003. Depends on T012.

### Observability (Constitution + research R-010)

- [ ] T023 [P] Add Serilog wiring in `backend/src/ExpenseTracker.Api/Program.cs` (JSON formatter prod / console dev, enriched with `CorrelationId`); add `CorrelationIdMiddleware` in `backend/src/ExpenseTracker.Api/Middleware/CorrelationIdMiddleware.cs` (read/issue `X-Correlation-Id`, push into `LogContext`).

### Swagger / OpenAPI

- [ ] T024 [P] Configure Swashbuckle in `Program.cs` with security scheme `bearerAuth`, XML doc inclusion, and emit `openapi.json` at `/swagger/v1/swagger.json`. Match the contract in `specs/001-expense-tracking-mvp/contracts/openapi.yaml`.

### Frontend foundation

- [ ] T025 [P] Add `frontend/src/app/domain/value-objects/{money.ts,month-year.ts}` and `frontend/src/app/domain/models/{user.ts,entry.ts,category.ts,currency.ts,monthly-view.ts,dashboard.ts}` mirroring data-model.md.
- [ ] T026 [P] Add `frontend/src/app/domain/ports/` interfaces: `auth.repository.ts`, `entries.repository.ts`, `months.repository.ts`, `dashboard.repository.ts`, `categories.repository.ts`, `currencies.repository.ts`, `admin.repository.ts`.
- [ ] T027 [P] Add `frontend/src/app/infrastructure/config/api.config.ts` (env-driven base URL `/api/v1`) and `frontend/src/app/infrastructure/http/http.module.ts` with provided `HttpClient`.
- [ ] T028 [P] Add `frontend/src/app/infrastructure/auth/jwt-token.store.ts` (sessionStorage-backed) and `frontend/src/app/infrastructure/auth/auth.interceptor.ts` (attach `Authorization: Bearer`, single-shot refresh on 401, then redirect to `/login`).
- [ ] T029 [P] Add `frontend/src/app/presentation/core/guards/auth.guard.ts` and `admin.guard.ts`; register routes shell in `frontend/src/app/app.routes.ts` (placeholders only).
- [ ] T030 [P] Add `frontend/src/app/presentation/shared/savings-rate.classifier.ts` (parity port of backend `SavingsRateClassifier` per research R-005) and unit test in `frontend/tests/unit/savings-rate.classifier.spec.ts`.

### Seed plumbing (used by stories 1, 6, 7)

- [ ] T031 Add `backend/src/ExpenseTracker.Infrastructure/Persistence/Seeding/Seeder.cs` and call it from `Program.cs` on startup; seeds Currencies and Categories from research R-014 and creates the bootstrap Admin user from `Seed:DefaultAdminEmail` / `Seed:DefaultAdminPassword`. No-op when data already exists. Depends on T013, T014.

**Checkpoint**: Empty API starts; Swagger renders; CORS, rate limiting, JWT pipeline configured; provider-factory wired; Angular shell routes resolve. **User-story phases may now begin.**

---

## Phase 3: User Story 1 — Register and Log In (Priority: P1) 🎯 MVP

**Story goal**: A visitor can register (name, phone, email, password, currency, optional photo) and log in by email-or-phone + password; on success they reach the (empty) Monthly View. Default role is `User`.

**Independent test**: Register a new account via the SPA, log in successfully, land on the current-month view shell, and verify the JWT is attached to a subsequent `GET /me` call returning the profile.

### Tests for User Story 1 ⚠️ (write first, ensure they FAIL before implementing)

- [ ] T032 [P] [US1] xUnit unit tests in `backend/tests/ExpenseTracker.Domain.Tests/ValueObjects/EmailAddressTests.cs` and `PhoneNumberTests.cs` covering validation rules.
- [ ] T033 [P] [US1] xUnit unit tests in `backend/tests/ExpenseTracker.Application.Tests/Auth/RegisterUserHandlerTests.cs` and `LoginUserHandlerTests.cs` (duplicate email/phone, wrong password, invalid currency, valid happy path).
- [ ] T034 [P] [US1] xUnit integration tests in `backend/tests/ExpenseTracker.Api.IntegrationTests/AuthEndpointsTests.cs` covering `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /me` against `WebApplicationFactory<Program>` with in-memory SQLite.
- [ ] T035 [P] [US1] Jest unit test in `frontend/tests/unit/application/register-user.use-case.spec.ts` and `login-user.use-case.spec.ts` using a fake `AuthRepository`.
- [ ] T036 [P] [US1] Playwright e2e in `frontend/tests/e2e/register-login.spec.ts` walking through register → login → land on Monthly View.

### Implementation for User Story 1

**Backend domain & application**

- [ ] T037 [P] [US1] Add `backend/src/ExpenseTracker.Domain/Entities/User.cs` (per data-model.md), `Role.cs` enum already in T009. Include factory `Create(...)` enforcing invariants.
- [ ] T038 [P] [US1] Add `backend/src/ExpenseTracker.Domain/Entities/UserProfilePhoto.cs`.
- [ ] T039 [P] [US1] Add `backend/src/ExpenseTracker.Domain/Entities/Currency.cs` (used by FK on User).
- [ ] T040 [P] [US1] Add `backend/src/ExpenseTracker.Domain/Entities/RefreshToken.cs`.
- [ ] T041 [P] [US1] Add repository abstractions in `backend/src/ExpenseTracker.Domain/Abstractions/`: `IUserRepository.cs`, `ICurrencyRepository.cs`, `IRefreshTokenRepository.cs`, `IUnitOfWork.cs`, `IPasswordHasher.cs`, `ITokenService.cs`, `IPhotoStorage.cs`.
- [ ] T042 [US1] Add DTOs `backend/src/ExpenseTracker.Application/Auth/Dtos/{RegisterUserRequest.cs,LoginRequest.cs,AuthResult.cs,UserProfileDto.cs,RefreshRequest.cs}` matching `contracts/openapi.yaml`.
- [ ] T043 [US1] Add FluentValidation validators in `backend/src/ExpenseTracker.Application/Auth/Validators/{RegisterUserValidator.cs,LoginValidator.cs}` (email format, phone E.164, password ≥ 8 chars + mix, currencyCode exists & active, photo MIME/size — leave photo dimension check to handler).
- [ ] T044 [US1] Implement use-case handlers in `backend/src/ExpenseTracker.Application/Auth/`: `RegisterUserHandler.cs` (hash via `IPasswordHasher`, validate currency via `ICurrencyRepository`, optional photo via `IPhotoStorage`), `LoginUserHandler.cs` (lookup by email **or** phone, verify hash, issue tokens, generic 401 on failure), `RefreshTokenHandler.cs` (rotate), `LogoutHandler.cs` (revoke).

**Backend infrastructure**

- [ ] T045 [US1] EF Core configurations under `backend/src/ExpenseTracker.Infrastructure/Persistence/Configurations/`: `UserConfiguration.cs`, `UserProfilePhotoConfiguration.cs`, `CurrencyConfiguration.cs`, `RefreshTokenConfiguration.cs` (unique indexes, FKs, lengths per data-model.md).
- [ ] T046 [US1] Implement repositories in `backend/src/ExpenseTracker.Infrastructure/Persistence/Repositories/`: `UserRepository.cs`, `CurrencyRepository.cs`, `RefreshTokenRepository.cs`; implement `UnitOfWork.cs` wrapping `AppDbContext.SaveChangesAsync`.
- [ ] T047 [P] [US1] Implement `backend/src/ExpenseTracker.Infrastructure/Identity/BcryptPasswordHasher.cs` (work-factor 12) and `JwtTokenService.cs` (issue access + opaque refresh, claims per research R-002).
- [ ] T048 [P] [US1] Implement `backend/src/ExpenseTracker.Infrastructure/Storage/DatabasePhotoStorage.cs` writing `UserProfilePhoto` using ImageSharp to validate dimensions (≤ 2048×2048). Reject unsupported MIME / oversize.
- [ ] T049 [US1] Generate EF migration `0001_Initial` under `backend/src/ExpenseTracker.Infrastructure/Persistence/Migrations/Sqlite/` covering Users, UserProfilePhotos, Currencies, RefreshTokens; verify `dotnet ef database update` succeeds; ensure Seeder (T031) populates currencies + bootstrap admin.

**Backend API**

- [ ] T050 [US1] Add `backend/src/ExpenseTracker.Api/Controllers/AuthController.cs` exposing `POST /auth/register` (multipart), `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`; annotate with `[AllowAnonymous]` where appropriate and `Authorize(Policy = PolicyNames.RequireUser)` for `/me`.
- [ ] T051 [US1] Add `backend/src/ExpenseTracker.Api/Controllers/MeController.cs` for `GET /me`, `PATCH /me`, `PUT /me/photo`, `DELETE /me/photo`, `GET /users/{id}/photo` (self or Admin); ensure ETag/Cache-Control per research R-007.
- [ ] T052 [US1] Add `backend/src/ExpenseTracker.Api/Controllers/CurrenciesController.cs` exposing `GET /currencies/active` as anonymous (used by registration form).

**Frontend**

- [ ] T053 [P] [US1] Implement `frontend/src/app/infrastructure/http/auth.http.repository.ts` calling the auth endpoints; provide via DI in `app.config.ts`.
- [ ] T054 [P] [US1] Implement `frontend/src/app/infrastructure/http/currencies.http.repository.ts` for `GET /currencies/active`.
- [ ] T055 [P] [US1] Implement `frontend/src/app/application/auth/{register-user.use-case.ts,login-user.use-case.ts,logout.use-case.ts}` and `frontend/src/app/application/auth/auth.state.ts` (Signals-based session state).
- [ ] T056 [US1] Build `frontend/src/app/presentation/features/auth/register/register.component.ts` (standalone, Reactive Forms, currency dropdown, optional photo, mirrors backend validators).
- [ ] T057 [US1] Build `frontend/src/app/presentation/features/auth/login/login.component.ts` (identifier + password, generic error message).
- [ ] T058 [US1] Add routes in `frontend/src/app/app.routes.ts`: `/register`, `/login`, `/` (current month, AuthGuard); add empty `monthly-view.component.ts` shell that renders "Monthly View — Story 2 to fill" so login lands successfully.

**Checkpoint US1**: Run e2e (T036). A new user can register, log in, see the Monthly View shell, and `/me` returns their profile. Bootstrap admin exists from the seeder. **MVP-of-the-MVP shippable.**

---

## Phase 4: User Story 2 — Record Expenses and Incomes for the Current Month (Priority: P1)

**Story goal**: Authenticated user can CRUD their own Income/Expense entries on any day of the current month using preconfigured categories or a free-text "Not listed" label; totals update on the screen within 1 s.

**Independent test**: As a logged-in user, create an entry via the form, verify it appears on the current-month list and that the displayed totals update; edit it; delete it; attempt to create an entry dated next month and verify the UI + API reject it.

### Tests for User Story 2 ⚠️

- [ ] T059 [P] [US2] xUnit tests `backend/tests/ExpenseTracker.Application.Tests/Entries/{CreateEntryHandlerTests.cs,UpdateEntryHandlerTests.cs,DeleteEntryHandlerTests.cs,ListEntriesByMonthHandlerTests.cs}` covering happy path, free-text category, category-type mismatch, amount ≤ 0, future-month rejection, ownership denial.
- [ ] T060 [P] [US2] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/EntriesEndpointsTests.cs` exercising `POST/PUT/DELETE/GET /entries` with JWT, including `EntryOwner` policy denial across users and `future_month_write_forbidden` 400 response.
- [ ] T061 [P] [US2] Jest unit tests `frontend/tests/unit/application/entries/{create-entry.use-case.spec.ts,update-entry.use-case.spec.ts,delete-entry.use-case.spec.ts}` with a fake repository.
- [ ] T062 [P] [US2] Playwright e2e `frontend/tests/e2e/record-entry.spec.ts` add → edit → delete in the current month + future-date rejection assertion.

### Implementation for User Story 2

**Backend domain & application**

- [ ] T063 [P] [US2] Add `backend/src/ExpenseTracker.Domain/Entities/Category.cs` and `Entry.cs` with invariants (amount > 0, future-month guard via `IClock`, category type must match entry type when linked, free-text label when unlinked).
- [ ] T064 [P] [US2] Add abstractions `backend/src/ExpenseTracker.Domain/Abstractions/{IEntryRepository.cs,ICategoryRepository.cs,IUserWriteCoordinator.cs}` (last interface for per-user serialization per research R-004).
- [ ] T065 [P] [US2] Add DTOs `backend/src/ExpenseTracker.Application/Entries/Dtos/{EntryCreateDto.cs,EntryUpdateDto.cs,EntryDto.cs}`; add validators `Validators/{EntryCreateValidator.cs,EntryUpdateValidator.cs}` (amount > 0, exclusive category vs. free-text, note ≤ 500 chars).
- [ ] T066 [US2] Add handlers `backend/src/ExpenseTracker.Application/Entries/{CreateEntryHandler.cs,UpdateEntryHandler.cs,DeleteEntryHandler.cs,GetEntryHandler.cs,ListEntriesByMonthHandler.cs}`. All write handlers: load entry → check ownership via `IAuthorizationService` (policy `EntryOwner`) → enforce future-month guard → snapshot `CategoryNameSnapshot` → persist via `IUnitOfWork` inside a serializable transaction → emit `EntryChanged` for Phase 6 (US4) to consume.

**Backend infrastructure**

- [ ] T067 [P] [US2] EF Core configurations `backend/src/ExpenseTracker.Infrastructure/Persistence/Configurations/{CategoryConfiguration.cs,EntryConfiguration.cs}` (indexes per data-model.md).
- [ ] T068 [P] [US2] Repositories `backend/src/ExpenseTracker.Infrastructure/Persistence/Repositories/{EntryRepository.cs,CategoryRepository.cs}`.
- [ ] T069 [P] [US2] Implement `backend/src/ExpenseTracker.Infrastructure/Concurrency/UserWriteCoordinator.cs` (per-UserId `SemaphoreSlim` keyed dictionary, singleton).
- [ ] T070 [US2] EF migration `0002_EntriesAndCategories` under `Persistence/Migrations/Sqlite/`; verify `dotnet ef database update`.

**Backend API**

- [ ] T071 [US2] `backend/src/ExpenseTracker.Api/Controllers/EntriesController.cs` exposing `POST /entries`, `GET /entries/{id}`, `PUT /entries/{id}`, `DELETE /entries/{id}`. Apply `[Authorize(Policy = PolicyNames.RequireUser)]`; perform resource-based authorization for `EntryOwner` after load.
- [ ] T072 [US2] `backend/src/ExpenseTracker.Api/Controllers/CategoriesController.cs` exposing `GET /categories?type=...` returning **active** categories visible to authenticated users.

**Frontend**

- [ ] T073 [P] [US2] `frontend/src/app/infrastructure/http/entries.http.repository.ts` and `categories.http.repository.ts`.
- [ ] T074 [P] [US2] `frontend/src/app/application/entries/{create-entry.use-case.ts,update-entry.use-case.ts,delete-entry.use-case.ts,list-entries-by-month.use-case.ts}` and `entries.state.ts` (Signal store keyed by `(year,month)`).
- [ ] T075 [US2] `frontend/src/app/presentation/features/entry-form/entry-form.component.ts` (standalone, Reactive Forms, category select with "Not listed Expense?/Income?" fallback that switches to free-text input). Includes client-side guard preventing date in next month (mirror backend rule).
- [ ] T076 [US2] Update `frontend/src/app/presentation/features/monthly-view/monthly-view.component.ts` to list entries by day with inline Add/Edit/Delete (modal opens `EntryFormComponent`). Reactively recomputes day-level subtotals; full monthly totals come from Story 3 endpoint but show stub values for now.

**Checkpoint US2**: A logged-in user can fully CRUD entries for the current month via UI and API; ownership/future-month rules enforced.

---

## Phase 5: User Story 3 — Monthly View with Totals and Savings (Priority: P1)

**Story goal**: For any current or previous month, the user sees a complete monthly view: opening balance, day-grouped entries, Total Income, Total Expense, Savings. Future months are read-only with projected opening balance.

**Independent test**: With seeded entries, open the current month and verify totals = sum of entries and savings = income − expense. Navigate to a previous month: same. Navigate to the next month: read-only view with projected opening balance and no entries.

### Tests for User Story 3 ⚠️

- [ ] T077 [P] [US3] xUnit unit tests `backend/tests/ExpenseTracker.Domain.Tests/Services/MonthlySummaryServiceTests.cs` covering totals computation including future-month projection (no entries, just opening balance).
- [ ] T078 [P] [US3] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/MonthsEndpointsTests.cs` for `GET /months/{year}/{month}` (current, past, future, read-only flag).
- [ ] T079 [P] [US3] Jest unit tests `frontend/tests/unit/application/months/get-monthly-view.use-case.spec.ts`.
- [ ] T080 [P] [US3] Playwright e2e `frontend/tests/e2e/monthly-view.spec.ts` (navigate months, verify totals and read-only future).

### Implementation for User Story 3

- [ ] T081 [P] [US3] Add `backend/src/ExpenseTracker.Domain/Entities/MonthlySummary.cs` and abstractions `IMonthlySummaryRepository.cs`.
- [ ] T082 [P] [US3] Add `backend/src/ExpenseTracker.Domain/Services/MonthlySummaryService.cs` (pure: given entries + previous closing, returns OpeningBalance/TotalIncome/TotalExpense/ClosingBalance/SavingsRatePct/StatusColor). Uses `SavingsRateClassifier`.
- [ ] T083 [US3] Add `backend/src/ExpenseTracker.Domain/Services/SavingsRateClassifier.cs` matching research R-005 thresholds (`Green ≥ 30`, `Orange < 30 && > 20`, `OrangeRedTint ≤ 20 && ≥ 10`, `BloodRed < 10 || income ≤ 0`).
- [ ] T084 [P] [US3] EF configuration `backend/src/ExpenseTracker.Infrastructure/Persistence/Configurations/MonthlySummaryConfiguration.cs` (composite PK `(UserId,Year,Month)`) and repository `Persistence/Repositories/MonthlySummaryRepository.cs`.
- [ ] T085 [US3] EF migration `0003_MonthlySummaries` under `Persistence/Migrations/Sqlite/`.
- [ ] T086 [US3] DTO `backend/src/ExpenseTracker.Application/Months/Dtos/MonthlyViewDto.cs` matching `MonthlyView` schema in `contracts/openapi.yaml`.
- [ ] T087 [US3] Handler `backend/src/ExpenseTracker.Application/Months/GetMonthlyViewHandler.cs`: load entries for `(user, year, month)`, read `MonthlySummary` row, if month > latest recorded month synthesize future-month projection (entries empty, totals 0, opening balance = last closing balance), set `readOnly = true` when in future, return DTO including `currencyCode = user.CurrencyCode`.
- [ ] T088 [US3] `backend/src/ExpenseTracker.Api/Controllers/MonthsController.cs` exposing `GET /months/{year}/{month}` with `[Authorize(Policy = PolicyNames.RequireUser)]`.
- [ ] T089 [P] [US3] `frontend/src/app/infrastructure/http/months.http.repository.ts`.
- [ ] T090 [P] [US3] `frontend/src/app/application/months/get-monthly-view.use-case.ts` and `months.state.ts` (Signal of currently-viewed MonthYear).
- [ ] T091 [US3] Wire totals + savings into `monthly-view.component.ts` (top bar: Total Income, Total Expense; bottom: Savings; left: Opening Balance). Disable Add/Edit/Delete actions when `readOnly = true` and show "future month — projected balance" banner.
- [ ] T092 [P] [US3] `frontend/src/app/presentation/features/monthly-view/month-switcher.component.ts` with Prev/Next/Today buttons updating `months.state` and triggering use case.

**Checkpoint US3**: Monthly view fully functional for past/current/future months; totals and savings displayed; performance budget SC-008 verifiable.

---

## Phase 6: User Story 4 — Carry-Forward of Balance Across Months (Priority: P1)

**Story goal**: Edits in any allowed month automatically recompute opening/closing balances for that month and every subsequent month, keeping the chain consistent.

**Independent test**: Add an entry in month N-1; verify N's opening balance updates. Edit the N-1 entry; verify N (and N+1 if present) re-update within 2 s. Confirm `m+1.OpeningBalance == m.ClosingBalance` for all months.

### Tests for User Story 4 ⚠️

- [ ] T093 [P] [US4] xUnit unit tests `backend/tests/ExpenseTracker.Domain.Tests/Services/CarryForwardCalculatorTests.cs` covering forward-walk correctness, missing months in between, negative balances, multi-month edit propagation.
- [ ] T094 [P] [US4] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/CarryForwardTests.cs` that POST entries across 3 months, verify GET `/months/...` for all 3 months returns coherent chain, then edit month 1 and re-verify within 2 s budget.

### Implementation for User Story 4

- [ ] T095 [P] [US4] Add `backend/src/ExpenseTracker.Domain/Services/CarryForwardCalculator.cs`: takes `(UserId, earliestAffectedMonth)` + entry repository + summary repository → walks forward to `max(MonthlySummary.MonthYear, latestEntryMonth) + 1`, recomputes summaries, persists.
- [ ] T096 [US4] Modify entry handlers from T066 to invoke `CarryForwardCalculator` inside the same `IUnitOfWork` transaction after every Create/Update/Delete (including handling moves: pass `min(oldMonth, newMonth)` as earliestAffected).
- [ ] T097 [US4] Wrap each entry-write handler with the `IUserWriteCoordinator` (T069) keyed on `UserId` to serialize concurrent recomputes per user.
- [ ] T098 [P] [US4] Extend `MonthsEndpointsTests` (T078) with a scenario that asserts `m+1.OpeningBalance == m.ClosingBalance` for every consecutive pair.
- [ ] T099 [US4] Frontend: after every successful entry mutation in `entries.state.ts` (T074), invalidate the months-state cache for the affected month and all later cached months so the next view re-fetches; reflect new opening balance on switch.

**Checkpoint US4**: Carry-forward chain is consistent and recomputes synchronously. Spec SC-003 verifiable.

---

## Phase 7: User Story 5 — Dashboard with Expense Graph and Carry-Forward Alerts (Priority: P2)

**Story goal**: Dashboard shows an expense trend graph and a carry-forward status indicator colored per the threshold table; prominent alert when expense ≥ income for the displayed month.

**Independent test**: Seed multi-month data and open the dashboard; verify graph renders trend, indicator matches the band per `SavingsRateClassifier`, and the alert appears when expense ≥ income.

### Tests for User Story 5 ⚠️

- [ ] T100 [P] [US5] xUnit unit tests `backend/tests/ExpenseTracker.Domain.Tests/Services/SavingsRateClassifierTests.cs` covering every band including zero-income → BloodRed.
- [ ] T101 [P] [US5] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/DashboardEndpointsTests.cs` for `GET /dashboard?monthsBack=N`.
- [ ] T102 [P] [US5] Jest unit tests `frontend/tests/unit/application/dashboard/get-dashboard.use-case.spec.ts` and `frontend/tests/unit/presentation/savings-rate-indicator.component.spec.ts` (color mapping per band).
- [ ] T103 [P] [US5] Playwright e2e `frontend/tests/e2e/dashboard.spec.ts` (verify graph renders, indicator color, expense-exceeds-income alert).

### Implementation for User Story 5

- [ ] T104 [P] [US5] DTOs `backend/src/ExpenseTracker.Application/Dashboard/Dtos/{DashboardDto.cs,DashboardMonthPointDto.cs}` matching `Dashboard` schema in `contracts/openapi.yaml`.
- [ ] T105 [US5] Handler `backend/src/ExpenseTracker.Application/Dashboard/GetDashboardHandler.cs`: load last `monthsBack` `MonthlySummary` rows for user + current month view; populate `alertExpenseExceedsIncome = currentMonth.TotalExpense >= currentMonth.TotalIncome && TotalIncome > 0` (and also true when income = 0 and expense > 0).
- [ ] T106 [US5] `backend/src/ExpenseTracker.Api/Controllers/DashboardController.cs` exposing `GET /dashboard?monthsBack=N` with `[Authorize(Policy = PolicyNames.RequireUser)]`.
- [ ] T107 [P] [US5] `frontend/src/app/infrastructure/http/dashboard.http.repository.ts`.
- [ ] T108 [P] [US5] `frontend/src/app/application/dashboard/get-dashboard.use-case.ts` and `dashboard.state.ts`.
- [ ] T109 [P] [US5] Install `ng2-charts` + `chart.js` in `frontend/package.json`; add `frontend/src/app/presentation/features/dashboard/expense-trend-chart.component.ts` (bar/line chart of per-month totals).
- [ ] T110 [US5] `frontend/src/app/presentation/features/dashboard/dashboard.component.ts` composing the chart, the `SavingsRateIndicatorComponent`, and the alert banner. Route: `/dashboard` (AuthGuard).
- [ ] T111 [P] [US5] `frontend/src/app/presentation/features/dashboard/savings-rate-indicator.component.ts` consuming `StatusColor` enum and rendering colored badge per research R-005.

**Checkpoint US5**: Dashboard end-to-end working; colors and alert match the spec thresholds.

---

## Phase 8: User Story 6 — Admin Manages Users (Priority: P2)

**Story goal**: Admin can list, view, edit, and (soft-)delete users. Non-admins are denied. Last-admin rule enforced.

**Independent test**: As bootstrap admin, list users (paginated, search), edit a user's profile fields, soft-delete a user, and verify they cannot log in. As a regular user, all admin endpoints return 403.

### Tests for User Story 6 ⚠️

- [ ] T112 [P] [US6] xUnit unit tests `backend/tests/ExpenseTracker.Application.Tests/Admin/{ListUsersHandlerTests.cs,UpdateUserHandlerTests.cs,DeleteUserHandlerTests.cs}` (last-admin enforcement, paging, search).
- [ ] T113 [P] [US6] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/AdminUsersEndpointsTests.cs` (Admin allowed, User denied 403, last-admin delete returns 409).
- [ ] T114 [P] [US6] Jest unit tests `frontend/tests/unit/application/admin/list-users.use-case.spec.ts` and the AdminGuard.
- [ ] T115 [P] [US6] Playwright e2e `frontend/tests/e2e/admin-users.spec.ts` (admin lists, edits, deletes; regular user gets 403).

### Implementation for User Story 6

- [ ] T116 [US6] Handlers `backend/src/ExpenseTracker.Application/Admin/Users/{ListUsersHandler.cs,GetUserHandler.cs,UpdateUserHandler.cs,DeleteUserHandler.cs}`. Delete enforces last-admin rule (FR-025) and supports `?hard=true` query for hard delete.
- [ ] T117 [US6] `backend/src/ExpenseTracker.Api/Controllers/AdminUsersController.cs` exposing `GET /admin/users`, `GET /admin/users/{id}`, `PATCH /admin/users/{id}`, `DELETE /admin/users/{id}?hard=` under `[Authorize(Policy = PolicyNames.RequireAdmin)]`.
- [ ] T118 [P] [US6] `frontend/src/app/infrastructure/http/admin.http.repository.ts` covering admin-user endpoints.
- [ ] T119 [P] [US6] `frontend/src/app/application/admin/users/{list-users.use-case.ts,update-user.use-case.ts,delete-user.use-case.ts}` + `admin-users.state.ts`.
- [ ] T120 [US6] `frontend/src/app/presentation/features/admin/users/users-list.component.ts` (paginated table, search) and `user-edit.component.ts` (form); route `/admin/users` guarded by `AdminGuard`.

**Checkpoint US6**: Admin user management UI and API operational; policy enforcement verified.

---

## Phase 9: User Story 7 — Admin Manages Categories and Currencies (Priority: P2)

**Story goal**: Admin can create / edit / deactivate Expense and Income categories; admin can create / edit / deactivate supported currencies. Changes appear in user-facing selectors; deactivation preserves historical labels.

**Independent test**: Admin creates a new Expense category; a regular user opens Add-Expense and sees it. Admin renames it; new name appears on next load. Admin deactivates it; it disappears from dropdowns while historical entries keep their snapshotted label. Same flow for currencies on the registration form.

### Tests for User Story 7 ⚠️

- [ ] T121 [P] [US7] xUnit unit tests `backend/tests/ExpenseTracker.Application.Tests/Admin/{ManageCategoryHandlerTests.cs,ManageCurrencyHandlerTests.cs}` (uniqueness, name preservation on entries after deactivation, currency referenced by users cannot be hard-deleted but can be deactivated).
- [ ] T122 [P] [US7] xUnit integration tests `backend/tests/ExpenseTracker.Api.IntegrationTests/{AdminCategoriesEndpointsTests.cs,AdminCurrenciesEndpointsTests.cs}` covering all admin endpoints and 403 for users.
- [ ] T123 [P] [US7] Jest unit tests `frontend/tests/unit/application/admin/{manage-category.use-case.spec.ts,manage-currency.use-case.spec.ts}`.
- [ ] T124 [P] [US7] Playwright e2e `frontend/tests/e2e/admin-categories-currencies.spec.ts` (admin CRUD + user dropdowns reflect changes).

### Implementation for User Story 7

- [ ] T125 [US7] Handlers `backend/src/ExpenseTracker.Application/Admin/Categories/{CreateCategoryHandler.cs,UpdateCategoryHandler.cs,DeactivateCategoryHandler.cs}` (deactivation = soft delete; preserve `Entry.CategoryNameSnapshot`).
- [ ] T126 [US7] Handlers `backend/src/ExpenseTracker.Application/Admin/Currencies/{CreateCurrencyHandler.cs,UpdateCurrencyHandler.cs,DeactivateCurrencyHandler.cs}` (deactivation hides from selectors; if any user still references it, refuse hard delete).
- [ ] T127 [US7] `backend/src/ExpenseTracker.Api/Controllers/AdminCategoriesController.cs` exposing `POST/PUT/DELETE /admin/categories[/{id}]` under `[Authorize(Policy = PolicyNames.RequireAdmin)]`.
- [ ] T128 [US7] `backend/src/ExpenseTracker.Api/Controllers/AdminCurrenciesController.cs` exposing `POST/PUT/DELETE /admin/currencies[/{code}]` under `[Authorize(Policy = PolicyNames.RequireAdmin)]`.
- [ ] T129 [P] [US7] Extend `admin.http.repository.ts` (T118) with categories + currencies endpoints.
- [ ] T130 [P] [US7] `frontend/src/app/application/admin/categories/{...}.use-case.ts` and `application/admin/currencies/{...}.use-case.ts` + states.
- [ ] T131 [US7] `frontend/src/app/presentation/features/admin/categories/categories-admin.component.ts` (table + add/edit/deactivate); route `/admin/categories` (AdminGuard).
- [ ] T132 [US7] `frontend/src/app/presentation/features/admin/currencies/currencies-admin.component.ts` (table + add/edit/deactivate); route `/admin/currencies` (AdminGuard).

**Checkpoint US7**: Admin can extend the global catalogs; users see updated dropdowns.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Quality, documentation, performance, and security work that spans all stories.

- [ ] T133 [P] Author per-project READMEs: `backend/src/ExpenseTracker.Domain/README.md`, `Application/README.md`, `Infrastructure/README.md`, `Api/README.md`, and `frontend/src/app/README.md` (Constitution IV requirement).
- [ ] T134 [P] Add XML doc comments to public types in Domain + Application; add TSDoc to `frontend/src/app/domain/` and `application/` public APIs.
- [ ] T135 [P] Verify `dotnet format --verify-no-changes` passes; verify `npm run lint` produces zero warnings; fix any drift.
- [ ] T136 [P] Add unit coverage gate ≥ 70% for Domain + Application (Coverlet) in CI; fail build under threshold.
- [ ] T137 Performance sanity check against SC-002 (entry write < 1 s), SC-003 (carry-forward < 2 s across 24 months), SC-008 (monthly view < 1.5 s with 200 entries × 12 months) using a seeded fixture; document results in `specs/001-expense-tracking-mvp/quickstart.md` Troubleshooting section.
- [ ] T138 Security hardening pass: confirm HTTPS redirect outside Dev; confirm secrets only via env/user-secrets; confirm 401/403/429 return ProblemDetails; run `dotnet list package --vulnerable` and `npm audit --production`; address any high/critical findings.
- [ ] T139 Diff the running API's emitted `swagger.json` against `specs/001-expense-tracking-mvp/contracts/openapi.yaml`; reconcile any drift (favor updating the contract only when the change is intentional and documented).
- [ ] T140 Run the `quickstart.md` acceptance walkthrough end-to-end on a clean clone; record any deviations and fix.

---

## Dependencies & Execution Order

### Phase dependencies

```
Phase 1: Setup ─┐
                ├─► Phase 2: Foundational ─┬─► Phase 3: US1 (P1) ─┐
                                           ├─► Phase 4: US2 (P1) ─┤
                                           ├─► Phase 5: US3 (P1) ─┤
                                           ├─► Phase 6: US4 (P1) ─┼─► Phase 10: Polish
                                           ├─► Phase 7: US5 (P2) ─┤
                                           ├─► Phase 8: US6 (P2) ─┤
                                           └─► Phase 9: US7 (P2) ─┘
```

### Cross-story dependencies (kept minimal)

- **US3** consumes `MonthlySummary` rows; until **US4** is implemented the summaries it reads are produced only by US2 writes (US2 should call a stub that recomputes the current month only; US4 replaces the stub with the full-chain `CarryForwardCalculator`). T066 explicitly leaves room for this; T096 closes the loop.
- **US5** depends on US3's `MonthlySummary` shape and US4's chain consistency for accurate trend.
- **US6** and **US7** are independent of US2–US5; they only need Phase 2 + US1's User entity and seeder.

### Within each user story

- Tests are written first and verified to fail before implementing.
- Backend: domain entity → repository abstraction → EF configuration & repository → handler → controller.
- Frontend: domain model → port → http repository → use case → state → component → route.
- Backend migration is run after configurations and before integration tests.

### Parallel opportunities (highlights)

- Phase 1 tasks T002–T008 are all `[P]` — run together.
- Phase 2 sub-areas (Domain primitives T009–T012, middleware T018–T024, frontend foundation T025–T030) can each progress in parallel; T013→T014→T031 form the persistence chain.
- Within each user-story phase, all `[P]` test tasks can be written in parallel; all `[P]` repository/configuration/use-case/state tasks can be done in parallel by different developers.
- US1, US6, and US7 are fully independent of each other and can be parallelized across three developers immediately after Phase 2.
- US2 → US3 → US4 form a tight chain; assign to one developer or to closely-coordinating pair.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Domain primitives — independent files, run in parallel:
Task: "T009 Enums in backend/src/ExpenseTracker.Domain/Common/Enums.cs"
Task: "T010 Value objects in backend/src/ExpenseTracker.Domain/ValueObjects/"
Task: "T011 IClock + SystemClock"
Task: "T012 Result/AppException/PolicyNames"

# Middleware/options — independent files, run in parallel:
Task: "T018 ExceptionHandlerMiddleware"
Task: "T019 CorsOptions + CORS wiring"
Task: "T020 RateLimitOptions + RateLimiter"
Task: "T021 JwtOptions + JwtBearer wiring"
Task: "T023 Serilog + CorrelationIdMiddleware"
Task: "T024 Swashbuckle config"

# Frontend foundation — independent files, run in parallel:
Task: "T025 Domain models"
Task: "T026 Domain ports"
Task: "T027 Api config + http module"
Task: "T028 Token store + interceptor"
Task: "T029 Guards + routes shell"
Task: "T030 Savings-rate classifier port"
```

## Parallel Example: User Story 1 tests

```bash
# All test tasks for US1 (different files):
Task: "T032 Domain VO tests"
Task: "T033 Application handler tests"
Task: "T034 API integration tests"
Task: "T035 Frontend use-case tests"
Task: "T036 Playwright e2e"
```

---

## Implementation Strategy

### MVP First (US1 → US2 → US3 → US4)

1. Complete Phase 1 + Phase 2.
2. Implement Phase 3 (US1). **Stop, validate, demo** registration + login.
3. Implement Phase 4 (US2). Validate single-month CRUD.
4. Implement Phase 5 (US3). Validate Monthly View with totals.
5. Implement Phase 6 (US4). Validate carry-forward correctness. **MVP shippable** — covers all P1 stories.

### Incremental Delivery beyond MVP

6. Implement Phase 7 (US5 — Dashboard) for analytics value.
7. Implement Phases 8 + 9 (US6 admin-users, US7 admin-categories+currencies) in parallel.
8. Phase 10 (Polish) — run continuously; close it out before tagging v1.0.

### Parallel Team Strategy

After Phase 2 completes:

- **Pair A**: US2 → US3 → US4 (the carry-forward backbone).
- **Pair B**: US1 polish + US5 (dashboard depends on US3/US4 reaching at least the summary table; can be drafted in parallel using fakes).
- **Pair C**: US6 + US7 (admin surface) — independent of the user-data backbone.

---

## Notes

- `[P]` = different files, no dependencies on incomplete tasks.
- `[Story]` label maps each task to its user story for traceability.
- Each phase has an explicit checkpoint that maps to a spec acceptance scenario.
- Tests are written first and verified to fail before implementation (constitution V).
- Commit per task or per logical group; CI gates (build, format, analyzers, tests, OpenAPI emit) MUST be green on every PR.
- Avoid: vague tasks, same-file conflicts, cross-story dependencies that break independence beyond the documented US2→US3→US4 chain.
