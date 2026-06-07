# Phase 0 Research: Expense Tracking MVP

**Date**: 2026-06-04
**Feature**: 001-expense-tracking-mvp

This document records the research and decisions taken to resolve every Technical Context unknown and to validate stack choices against the project Constitution. All `NEEDS CLARIFICATION` markers in the spec were resolved during `/speckit.clarify` (5 questions answered); this phase resolves remaining technical-design questions.

---

## R-001 — Pluggable EF Core Provider Selection

**Decision**: Implement a `DbContextOptionsFactory` (Factory pattern) registered as a singleton. It reads `Database:Provider` (`Sqlite` | `SqlServer` | `PostgreSQL` | `MySQL`) and `ConnectionStrings:Default` from configuration, and returns `DbContextOptions<AppDbContext>` configured with the matching provider. `AppDbContext` is provider-agnostic; provider-specific quirks are handled in `IEntityTypeConfiguration<T>` classes guarded by `Database.IsSqlite()` / `Database.IsSqlServer()` etc. Migrations live under `Infrastructure/Persistence/Migrations/{Provider}/` and are selected via `--context AppDbContext --output-dir`.

**Rationale**: Satisfies Constitution Principle II (plug-and-play persistence) without source-code changes outside the composition root. The Factory is the single switch point.

**Alternatives considered**:
- *Multiple `DbContext` types per provider*: Higher maintenance cost; rejected.
- *Generic-host environment variables only without a Factory*: Less testable; harder to swap in tests; rejected.
- *Conditional compilation per provider*: Violates "no environment-specific code branches" (constitution).

---

## R-002 — Authentication & Identity Stack

**Decision**: Use **`Microsoft.AspNetCore.Authentication.JwtBearer`** for token validation; issue tokens via a custom `ITokenService` implementation. Hash passwords with **BCrypt** (`BCrypt.Net-Next`) at work-factor 12. Do **not** adopt full ASP.NET Core Identity for the MVP — the spec's identity model is small (User + Role enum, email/phone login) and Identity's schema would add migration complexity to support pluggable providers.

**Rationale**: Minimal dependency footprint; aligns with Principle III (security-first) and Principle II (provider-agnostic schema). Custom user table keeps the `Currency` FK and timezone field clean.

**Alternatives considered**:
- *ASP.NET Core Identity*: powerful but introduces ~7 tables and EF migrations that are awkward across providers; overkill for two roles and email/phone login.
- *Argon2id via `Konscious.Security.Cryptography`*: stronger than BCrypt but adds a less-mainstream dependency. BCrypt at cost 12 is acceptable for MVP; documented as a future upgrade path.
- *Cookie auth + anti-forgery*: rejected because the SPA + mobile-friendly future expansion benefits from bearer tokens.

**Token shape**: Access tokens valid 30 min; claims = `sub` (UserId), `role`, `currency` (ISO code), `email`. Refresh tokens (opaque, server-side stored) valid 7 days, rotated on use, revocable on logout / admin delete.

---

## R-003 — Policy-Based Authorization Design

**Decision**: Define named authorization policies registered in `Program.cs` and applied via `[Authorize(Policy = "...")]`:

- `RequireAdmin` — requires `role == Admin`.
- `RequireUser` — requires authenticated user (any role).
- `EntryOwner` — resource-based handler `EntryOwnerAuthorizationHandler : AuthorizationHandler<EntryOwnerRequirement, Entry>` that compares `Entry.UserId` with `ClaimTypes.NameIdentifier`. Admins also pass.

Endpoints that touch a user's own data first load the resource, then call `IAuthorizationService.AuthorizeAsync(user, entry, "EntryOwner")`. This satisfies FR-013 (user-only access to own entries) and FR-005 (policy-based authorization).

**Alternatives considered**:
- *Role-only `[Authorize(Roles="Admin")]`*: insufficient — does not handle resource ownership.
- *Filter-based ownership in repositories*: hidden authorization; harder to audit; rejected.

---

## R-004 — Carry-Forward Recompute Algorithm

**Decision**: Maintain a `MonthlySummary` table cached per `(UserId, Year, Month)` with `OpeningBalance`, `TotalIncome`, `TotalExpense`, `ClosingBalance`, `SavingsRate`, `StatusColor`. On any write to an `Entry` for a (UserId, MonthYear), recompute that month's totals from current entries inside the same DB transaction, then walk forward month-by-month updating `OpeningBalance := previous.ClosingBalance` and `ClosingBalance := OpeningBalance + TotalIncome − TotalExpense` until the latest month with data (and one synthetic month ahead if the dashboard projects a future opening balance). The walk stops at the user's max recorded month.

For SC-003 (≤ 2 s for typical history of ≤ 24 months), pure SQL aggregation per month plus an in-memory walk is well within budget.

**Alternatives considered**:
- *Compute on read every time, no cache*: simpler but blows SC-008 (Monthly View < 1.5 s) when 24 months × 200 entries.
- *Materialized view*: provider-specific; conflicts with Principle II.
- *Async background recompute*: violates SC-003's strict consistency guarantee.

**Concurrency**: Use a per-user lightweight semaphore (`IUserWriteCoordinator` keyed by UserId) plus EF Core `SaveChangesAsync` in a serializable transaction to prevent interleaved recompute.

---

## R-005 — Savings-Rate Color Classifier

**Decision**: A pure domain service `SavingsRateClassifier.Classify(decimal totalIncome, decimal totalExpense) : StatusColor` returns one of `{ Green, Orange, OrangeRedTint, BloodRed }` per FR-021/FR-023:

```
if totalIncome <= 0: BloodRed
rate = (totalIncome - totalExpense) / totalIncome * 100
if rate >= 30: Green
elif rate > 20: Orange
elif rate >= 10: OrangeRedTint
else: BloodRed
```

Documented as the single source of truth; UI consumes the enum and maps to CSS classes. Frontend MUST also implement the same function for instant updates and have a unit test checking parity with the API output.

**Alternatives considered**: Server-only classification with no client mirror — would block the UX requirement to show the band immediately on add/edit (SC-002).

---

## R-006 — Future-Month Projection

**Decision**: When a User requests a month after the latest month with entries, the API returns a synthesized response:
- `entries: []`
- `totalIncome: 0`
- `totalExpense: 0`
- `openingBalance: previousMonth.closingBalance` (computed by walking forward from the last known month, treating intermediate months as zero-activity)
- `readOnly: true`

Writes to a future-month date return `400 Problem+JSON` with code `future_month_write_forbidden`. (FR-011, FR-016)

**Alternatives considered**: 404 for future months — rejected because UX requires showing the projected opening balance.

---

## R-007 — Profile Photo Storage

**Decision**: Store profile photos as `byte[]` in a `UserProfilePhoto` table with `(UserId PK/FK, ContentType, Bytes, UpdatedAt)`. Serve via `GET /users/{id}/photo` with `Cache-Control: private, max-age=300` and an `ETag`. Validate at upload: MIME `image/jpeg|image/png`, size ≤ 2 MB, dimensions ≤ 2048×2048 (decoded with `SkiaSharp` or `System.Drawing` on Linux via `Microsoft.Maui.Graphics` — *or* `ImageSharp` to avoid GDI+ on Linux).

**Rationale**: MVP simplicity; no external blob store dependency; backups inherit DB backup. Migration to S3/Azure Blob later requires only swapping the `IPhotoStorage` adapter.

**Library choice**: `SixLabors.ImageSharp` for cross-platform image dimension validation (Linux containers lack GDI+ by default).

**Alternatives considered**:
- *Filesystem*: complicates container redeploys and horizontal scaling; rejected for MVP.
- *External blob store*: scope creep; revisit in v2.

---

## R-008 — Currency Catalog & User Currency

**Decision**:
- `Currency` table: `Code (PK, ISO 4217 char(3))`, `Symbol`, `DecimalPlaces`, `IsActive`, `CreatedAt`, `UpdatedAt`.
- `User.CurrencyCode` FK (`ON DELETE RESTRICT`); `User.CurrencyCode` MUST reference an existing currency at registration.
- `Entry.Amount` is `decimal(18, 4)`; UI rounds to `Currency.DecimalPlaces` for display.
- No FX conversion: changing `User.CurrencyCode` changes display symbol only.
- Seed at first migration: `INR (₹, 2)`, `USD ($, 2)`, `EUR (€, 2)`, `GBP (£, 2)`, `JPY (¥, 0)`. Admins can extend or deactivate.

**Rationale**: Clean separation; deactivation preserves historical data; ISO code as PK avoids accidental name drift.

**Alternatives considered**: Per-entry currency — rejected; spec assumes a per-user currency.

---

## R-009 — Rate Limiting Policies

**Decision**: Use ASP.NET Core's built-in `RateLimiter` middleware:

- Global per-IP fixed-window: 100 req / 1 min.
- `auth` partition (login + register): per-IP 5 req / 1 min, sliding window, queue 0 (immediate 429).
- `authenticated` partition (post-JWT): per-User token-bucket 60 req / 1 min, burst 30.

Configured via `RateLimitOptions` bound from `appsettings.json` so values are environment-driven (no code changes per env). Returns RFC 7807 ProblemDetails on rejection.

---

## R-010 — Observability

**Decision**: Use **Serilog** with `Serilog.AspNetCore`, `Serilog.Sinks.Console` (JSON formatter for production, human-readable for dev), and `Serilog.Enrichers.CorrelationId`. Add a middleware that issues/propagates `X-Correlation-Id` and pushes it into the `LogContext`. Application-layer use cases log structured events (`UseCase`, `UserId`, `Outcome`).

**Rationale**: Constitution mandates structured logging and correlation IDs flowing through application logic.

**Alternatives considered**: OpenTelemetry — desirable but defer to v2; Serilog covers MVP needs and OTEL bridging is straightforward later.

---

## R-011 — Frontend Architecture & Charts

**Decision**:
- Angular **standalone components**, modern control flow (`@if`, `@for`, `@switch`), Signals for component state, RxJS only at the infrastructure boundary (HTTP).
- Folder layout enforces Clean Architecture: `domain/`, `application/`, `infrastructure/`, `presentation/`. ESLint rule (`eslint-plugin-boundaries` or path-based custom rule) blocks imports from outer→inner.
- **State**: Service-with-Signal at first; do not adopt NgRx in MVP (justify in plan if added later).
- **Charts**: `ng2-charts` (Chart.js v4) — small footprint, well-supported, sufficient for the per-month expense trend and per-category breakdown.
- **Forms**: Reactive Forms with validators mirrored from the backend FluentValidation rules.
- **Auth interceptor**: attaches `Authorization: Bearer <jwt>`, refreshes on 401 once via the refresh-token endpoint, then redirects to `/login` on failure.

**Rationale**: Standalone + signals are the canonical Angular 21 path; ng2-charts has the smallest learning curve for this MVP's chart types.

**Alternatives considered**:
- *NgRx + effects*: overkill for MVP scale; revisit if state complexity grows.
- *@swimlane/ngx-charts*: SVG-based, declarative; rejected only because Chart.js examples for time-series and multi-axis are more abundant.

---

## R-012 — Testing Strategy

**Decision**:
- **Domain**: pure unit tests for `CarryForwardCalculator`, `SavingsRateClassifier`, value objects. No I/O.
- **Application**: unit tests with in-memory fakes of repository ports.
- **Infrastructure / API**: integration tests with `WebApplicationFactory<Program>` swapping the SQLite connection to `Data Source=:memory:` (held open for the test lifetime). Real DbContext, real middleware, fake clock.
- **Frontend**: Jest unit tests for use cases and components; Playwright e2e for "register → record entry → see totals" and "admin manages categories".
- Coverage gate: ≥ 70% line coverage for Domain + Application; smoke coverage for Infra/API/UI.

---

## R-013 — Configuration Schema

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "Default": "Data Source=expense.db" },
  "Jwt": {
    "Issuer": "expense-tracker",
    "Audience": "expense-tracker-clients",
    "AccessTokenMinutes": 30,
    "RefreshTokenDays": 7,
    "SigningKey": "<env: JWT__SIGNINGKEY>"
  },
  "Cors": { "AllowedOrigins": ["http://localhost:4200"] },
  "RateLimit": {
    "GlobalPerMinute": 100,
    "AuthPerMinute": 5,
    "AuthenticatedPerMinute": 60
  },
  "Photo": { "MaxBytes": 2097152, "MaxDimension": 2048, "AllowedMimeTypes": ["image/jpeg","image/png"] },
  "Seed": { "DefaultAdminEmail": "<env>", "DefaultAdminPassword": "<env>" }
}
```

Secrets (`Jwt__SigningKey`, admin password, prod connection string) come from environment variables or user secrets — never source.

---

## R-014 — Initial Seed Data

**Decision**: On first run, the API seeds:
- Currencies: INR, USD, EUR, GBP, JPY (active).
- Expense categories: Food, Transport, Rent, Utilities, Entertainment, Health, Education, Other.
- Income categories: Salary, Bonus, Interest, Gift, Other.
- A single Admin user from `Seed:DefaultAdminEmail` / `Seed:DefaultAdminPassword` (env vars). Required at first start; refuses to start without them in non-Development if no Admin exists.

**Rationale**: Satisfies FR-007 (preconfigured categories) and the constitution's "at least one Admin must exist".

---

## Summary

All Technical Context items resolved. No outstanding `NEEDS CLARIFICATION`. Constitution Check passes. Ready for Phase 1 design artifacts.
