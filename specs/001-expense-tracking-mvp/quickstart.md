# Quickstart: Expense Tracking MVP

**Date**: 2026-06-04
**Feature**: 001-expense-tracking-mvp

This quickstart explains how a developer brings the solution up locally and runs the primary acceptance flow end-to-end. Follow [plan.md](./plan.md) for architecture rationale and [contracts/openapi.yaml](./contracts/openapi.yaml) for the full API contract.

---

## Prerequisites

- **.NET SDK 10.0** (`dotnet --version` ≥ 10.0)
- **Node.js 20 LTS** + npm 10+
- **Angular CLI** (installed automatically via the workspace's devDependencies)
- (Optional) **VS Code** with the C# Dev Kit and Angular Language Service extensions

No external database or services are required for local dev; the default SQLite file is created automatically.

---

## 1) Clone & restore

```bash
git checkout 001-expense-tracking-mvp
cd backend && dotnet restore
cd ../frontend && npm install
```

## 2) Configure secrets (one-time, dev)

The API requires a JWT signing key and a bootstrap admin. Use dotnet user-secrets:

```bash
cd backend/src/ExpenseTracker.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)"
dotnet user-secrets set "Seed:DefaultAdminEmail" "admin@example.local"
dotnet user-secrets set "Seed:DefaultAdminPassword" "ChangeMe!Now123"
```

Default `appsettings.Development.json` already sets `Database:Provider=Sqlite` and `ConnectionStrings:Default=Data Source=expense.db`.

## 3) Apply migrations & run the API

```bash
cd backend/src/ExpenseTracker.Api
dotnet ef database update --project ../ExpenseTracker.Infrastructure
dotnet run
```

The API listens on `https://localhost:5001` (or configured port). Swagger UI is at `/swagger`. The health probe is `GET /api/v1/health`.

On first run the API seeds:
- Currencies: INR, USD, EUR, GBP, JPY
- Expense categories: Food, Transport, Rent, Utilities, Entertainment, Health, Education, Other
- Income categories: Salary, Bonus, Interest, Gift, Other
- Bootstrap admin from `Seed:DefaultAdminEmail` / `Seed:DefaultAdminPassword`

## 4) Run the Angular app

```bash
cd frontend
npm start
```

The SPA serves at `http://localhost:4200` and is configured to call the API at `https://localhost:5001/api/v1` (configurable in `src/app/infrastructure/config/api.config.ts`).

---

## 5) Acceptance walkthrough (matches spec User Stories 1–5)

### US-1: Register & log in
1. Visit `http://localhost:4200/register`.
2. Submit first/last name, **unique** email and phone, password (≥ 8 chars), and pick a currency (e.g. `USD`). Photo is optional.
3. Verify redirect to `/login`, then sign in with email **or** phone + password. Confirm the JWT lands in storage and the Monthly View opens for the current month.

### US-2: Record entries
1. On the Monthly View, click **Add Expense** for today. Pick a category (e.g. *Food*) and an amount.
2. Confirm the entry appears under today's date and `Total Expense` and `Savings` update within ~1 s.
3. Choose **Add Income** with category *Salary*. Confirm `Total Income` updates.
4. Try **Add Expense** with the date set to a day in **next month** — the form must reject it (UI guard) and the API would return `400` with `code=future_month_write_forbidden`.
5. Edit and delete entries; verify totals refresh.

### US-3: Monthly navigation
1. Use the month switcher to navigate to the **previous month**. CRUD must remain available.
2. Navigate to the **next month** — the view is read-only, shows projected `Opening Balance`, no entries, no add/edit buttons.

### US-4: Carry-forward
1. Add an income in the previous month. Navigate to the current month — `Opening Balance` increases by the same amount.
2. Edit the previous-month entry's amount; current month's `Opening Balance` updates within ~2 s.

### US-5: Dashboard
1. Open the Dashboard. The expense graph renders the trend; the carry-forward indicator displays one of `Green / Orange / OrangeRedTint / BloodRed` based on the savings rate.
2. Add expenses until they reach or exceed income for the displayed month — the prominent expense-exceeds-income alert appears.

### US-6 / US-7: Admin
1. Log in as the bootstrap admin.
2. Visit `/admin/users` — list, edit, delete users.
3. Visit `/admin/categories` — add/rename/deactivate categories. Verify they show or hide in the User dropdowns.
4. Visit `/admin/currencies` — add a new currency; verify it appears in registration's currency selector.

---

## 6) Tests

```bash
# Backend unit + integration
cd backend
dotnet test

# Frontend unit
cd ../frontend
npm test

# Frontend e2e (requires API + SPA running)
npm run e2e
```

CI gates that block PR merge:
- `dotnet build /p:TreatWarningsAsErrors=true` (Release)
- `dotnet format --verify-no-changes`
- `dotnet test`
- `ng build --configuration=production`
- `npm run lint`
- `npm test -- --watch=false`

---

## 7) Switching the database provider

Persistence is plug-and-play (Constitution Principle II). Example: switch to PostgreSQL.

1. Add the provider package once: `dotnet add backend/src/ExpenseTracker.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Add migrations under `Infrastructure/Persistence/Migrations/PostgreSQL/`:
   `dotnet ef migrations add Initial --project backend/src/ExpenseTracker.Infrastructure --output-dir Persistence/Migrations/PostgreSQL`
3. Configure at runtime via environment:
   ```bash
   export Database__Provider=PostgreSQL
   export ConnectionStrings__Default="Host=...;Database=expense;Username=...;Password=..."
   dotnet run --project backend/src/ExpenseTracker.Api
   ```

No source changes outside the composition root are required.

---

## 8) Troubleshooting

- **`401 Unauthorized` on every API call** — verify the JWT signing key is set; the dev SPA only stores tokens for the current browser session.
- **`SQLite Error 1: 'no such table: Users'`** — migrations not applied; re-run step 3.
- **CORS error in browser** — confirm `Cors:AllowedOrigins` in `appsettings.Development.json` includes `http://localhost:4200`.
- **Photo upload `400`** — file must be JPEG/PNG, ≤ 2 MB, ≤ 2048×2048.
- **Cannot demote/delete last admin** — by design (FR-025); promote another user first.
