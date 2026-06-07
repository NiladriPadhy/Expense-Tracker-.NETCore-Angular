# Feature Specification: Expense Tracking MVP (Web App + APIs)

**Feature Branch**: `001-expense-tracking-mvp`

**Created**: 2026-06-04

**Status**: Draft

## Clarifications

### Session 2026-06-04

- Q: What is the second accepted login identifier alongside phone? → A: Email or phone (no separate username field); login uses email **or** phone with password.
- Q: Which formula drives the dashboard carry-forward color bands? → A: (Savings ÷ Total Income) × 100, where Savings = Total Income − Total Expense for the displayed month (monthly savings rate).
- Q: How should future months behave in the UI/API? → A: Read-only view allowed; projected opening balance shown; no entries listed; no add/edit/delete actions; writes rejected by the API.
- Q: How is currency handled? → A: Admin maintains the catalog of **supported currencies** (code, symbol, decimal places); each user **selects their currency** at registration (and can change it in profile); a user's totals/entries are stored and displayed in that user's selected currency. No FX/conversion in MVP.
- Q: Profile photo accepted formats and limits? → A: **JPEG and PNG only, max 2 MB, max 2048×2048 pixels**; uploads outside these bounds are rejected with a clear validation message and account creation may still proceed without a photo.

**Input**: User description: "Angular App with user registration (phone, email, password, first name, last name, profile photo); login by username/phone and password; preconfigured expense and income categories with a 'Not listed' free-text fallback; full CRUD for expenses and incomes on any day of a month; monthly view showing total income and total expense at the top and savings (income − expense) at the bottom; cannot add expenses for next month; CRUD allowed for current and previous months; edits to past months must propagate to future months via balance carry-forward; dashboard with an expense graph and an alert when expense approaches income or carry-forward drops; carry-forward thresholds: ≥30% green, <30% orange, ≤20% orange with red tint, <10% blood red. Two roles: Admin (manage all users, manage preconfigured categories) and User (select category). Policy-based authorization."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Register and Log In (Priority: P1)

A new visitor creates an account with personal details and a chosen credential, then signs in to access their personal monthly expense workspace.

**Why this priority**: Without authenticated identity, no per-user financial data can be stored, viewed, or protected. This is the gateway to every other feature.

**Independent Test**: A new user can complete registration, log in, and reach an empty Monthly View — verifying the auth flow end-to-end without depending on any other story.

**Acceptance Scenarios**:

1. **Given** a visitor on the registration page, **When** they submit valid first name, last name, phone, email, password, a **selected currency** from the supported list, and (optionally) a profile photo, **Then** an account is created with that currency as the user's default and they are redirected to the login page (or auto-logged-in) with the User role assigned by default.
2. **Given** an existing user, **When** they submit a correct identifier (email or phone) and password on the login page, **Then** they receive an authenticated session and are taken to the current month's view.
3. **Given** invalid credentials, **When** login is attempted, **Then** the system rejects the attempt with a generic error message and does not disclose which field was wrong.
4. **Given** registration with a phone or email already in use, **When** the user submits, **Then** registration is rejected with a clear validation message.

---

### User Story 2 — Record Expenses and Incomes for the Current Month (Priority: P1)

The signed-in user adds, edits, and deletes expenses and incomes for any day of the current month, choosing a preconfigured category or entering free text when the category is not listed.

**Why this priority**: Capturing daily transactions is the core utility of the product. Everything else (totals, carry-forward, dashboard) depends on this data.

**Independent Test**: A logged-in user with no other features available can add, edit, and delete several entries for the current month and see the running totals update — delivering the core "expense tracker" value.

**Acceptance Scenarios**:

1. **Given** a logged-in user on the current-month view, **When** they add an expense for any past or future day **of the current month** with category "Food" and amount, **Then** the entry appears under that day, the Total Expense at the top updates, and Savings at the bottom recalculates.
2. **Given** the user is creating an entry, **When** their category is not in the preconfigured list, **Then** they select "Not listed Expense?" / "Not listed Income?" and provide a free-text category which is saved with that entry.
3. **Given** an existing entry, **When** the user edits the amount, date (within allowed months), category, or note, **Then** the change is persisted and totals refresh.
4. **Given** an existing entry, **When** the user deletes it, **Then** it is removed and totals refresh.
5. **Given** the current month, **When** the user attempts to add an expense or income dated in the **next** month, **Then** the system prevents the action with a clear message.

---

### User Story 3 — Monthly View with Totals and Savings (Priority: P1)

The user navigates between months and sees, for the selected month, total income at the top, total expense at the top, and savings (income − expense) at the bottom, alongside the day-by-day list of entries.

**Why this priority**: This is the primary workspace UI; without it, recorded data has no place to be reviewed or managed.

**Independent Test**: With seeded entries, the user opens the monthly view, verifies the three totals match the entries, and switches months to see the per-month figures.

**Acceptance Scenarios**:

1. **Given** entries exist for a month, **When** the user opens the monthly view, **Then** Total Income and Total Expense are shown at the top and Savings = Total Income − Total Expense is shown at the bottom.
2. **Given** the user is on the current month, **When** they navigate to a previous month, **Then** the view shows that month's entries and totals and CRUD remains available.
3. **Given** the user is on the current month, **When** they attempt to navigate to and edit the **next** month, **Then** the next-month view is read-only or blocked from data entry.

---

### User Story 4 — Carry-Forward of Balance Across Months (Priority: P1)

The user's net savings from one month carries forward as the opening balance of the next month, and any retroactive edit in a previous month must propagate to all future months automatically.

**Why this priority**: Carry-forward is what turns a list of monthly snapshots into a coherent personal-finance picture, and it is required for the dashboard alert thresholds to be meaningful.

**Independent Test**: Add income and expenses across two consecutive months, verify month 2 shows the correct opening balance from month 1; then edit a month 1 entry and confirm month 2's opening balance and onward chain update automatically.

**Acceptance Scenarios**:

1. **Given** month N has Total Income I and Total Expense E and prior carry-in C, **When** month N+1 is opened, **Then** month N+1 shows an opening balance of (C + I − E).
2. **Given** the user edits, adds, or deletes an entry in a previous month, **When** the change is saved, **Then** the carry-forward chain recomputes and every later month reflects the new opening balance and dashboard color.
3. **Given** the carry-forward into a month is negative, **When** the month is displayed, **Then** the opening balance is shown as a clearly labelled negative value (no silent rounding to zero).

---

### User Story 5 — Dashboard with Expense Graph and Carry-Forward Alerts (Priority: P2)

The user sees a dashboard with a graph of expenses and a status indicator that warns them when expenses approach income or when carry-forward drops below configured thresholds.

**Why this priority**: Dashboard insight is a major value-add but depends on Stories 2–4 already producing data; it is therefore P2 rather than P1.

**Independent Test**: With seeded multi-month data, the dashboard renders an expense chart and the carry-forward indicator displays the correct color band per the threshold table; an alert appears when expenses reach or exceed income for a month.

**Acceptance Scenarios**:

1. **Given** the user has data in one or more months, **When** they open the dashboard, **Then** they see an expense graph (per-month totals and/or per-category breakdown for the selected month) and a carry-forward indicator.
2. **Given** the carry-forward percentage is **≥ 30%**, **When** the indicator renders, **Then** it is shown in **green**.
3. **Given** the carry-forward percentage is **< 30% and > 20%**, **When** the indicator renders, **Then** it is shown in **orange**.
4. **Given** the carry-forward percentage is **≤ 20% and ≥ 10%**, **When** the indicator renders, **Then** it is shown in **orange with a red tint**.
5. **Given** the carry-forward percentage is **< 10%**, **When** the indicator renders, **Then** it is shown in **blood red** and an alert message is displayed.
6. **Given** the user's monthly expense reaches or exceeds monthly income, **When** the dashboard renders, **Then** a prominent warning alert is shown regardless of carry-forward color.

---

### User Story 6 — Admin Manages Users (Priority: P2)

An Admin can list all users, edit user profile fields, and delete users. Non-admins cannot access these capabilities.

**Why this priority**: Required for operational control of the system but not on the critical path for the end-user MVP value loop.

**Independent Test**: A user with the Admin role can list, edit, and delete users via dedicated screens/endpoints; a User-role account receives an authorization failure on the same actions.

**Acceptance Scenarios**:

1. **Given** an Admin is signed in, **When** they open the Users screen, **Then** they see a paginated list of all users with name, email, phone, and role.
2. **Given** an Admin views a user, **When** they edit profile fields and save, **Then** the changes persist.
3. **Given** an Admin deletes a user, **When** they confirm, **Then** the user account is removed (or soft-deleted) and the user can no longer log in; the user's transaction data is handled per the documented retention rule (see Assumptions).
4. **Given** a User-role account, **When** they call any user-management action, **Then** the system returns an authorization failure (403-equivalent) and the UI does not display the admin screens.

---

### User Story 7 — Admin Manages Preconfigured Categories & Currencies (Priority: P2)

An Admin can add, edit, and delete (a) preconfigured Expense and Income categories that all Users see in their dropdowns, and (b) the catalog of supported currencies that Users can choose from at registration or in profile settings.

**Why this priority**: Necessary to keep the category and currency catalogs current. Users can already work via free-text "Not listed" entries while categories evolve, and at least one currency will be seeded, so it is P2.

**Independent Test**: An Admin creates a new Expense category and a new currency; a User opens the Add-Expense form and sees the new category, and the registration form shows the new currency. Admin renames or deletes either; the changes are reflected for Users on next load while historical entries retain their original labels.

**Acceptance Scenarios**:

1. **Given** an Admin opens Category management, **When** they create an Expense or Income category with a unique name, **Then** the category is available in all Users' dropdowns.
2. **Given** an Admin renames a category, **When** they save, **Then** the new name is used going forward and historical entries continue to reference the same category record.
3. **Given** an Admin deletes a category, **When** they confirm, **Then** the category is no longer offered for new entries; existing entries are preserved (see Assumptions for retention behavior).
4. **Given** an Admin opens Currency management, **When** they add a currency (code, symbol, decimal places), **Then** it appears in the registration and profile currency selectors for Users.
5. **Given** an Admin deactivates a currency in use by existing users, **When** they confirm, **Then** the currency is hidden from new selections but those users keep their existing currency on stored entries until they choose a different supported one.
6. **Given** a User-role account, **When** they call category- or currency-management actions, **Then** authorization is denied.

---

### Edge Cases

- A user has zero income for a month but recorded expenses → carry-forward percentage is undefined; treat as **< 10%** (blood red) and show a "no income recorded" hint.
- A user has zero income and zero expense → indicator is neutral/green and no alert is shown.
- A user changes the system clock or timezone → "current month" is determined by server time/date in the user's stored timezone (default: UTC) to prevent skipping the next-month restriction.
- A user uploads a profile photo exceeding size or invalid format → upload is rejected with a clear validation message; account creation can still proceed without a photo.
- An entry with a free-text "Not listed" category is created, then later the Admin adds a matching preconfigured category → the historical entry is **not** auto-relinked; it stays as free text.
- An Admin deletes a category that is in use → existing entries keep a snapshot of the category name (label preserved) but the category is removed from selection lists.
- A user attempts to add an entry for a date that does not exist in the chosen month (e.g., Feb 30) → the date picker prevents this; server rejects out-of-range dates.
- An Admin deletes their own account → action is blocked; at least one Admin must remain.
- Long-running carry-forward recompute after a far-past edit → the operation completes synchronously for the affected user's data and totals are consistent before the response returns.

## Requirements *(mandatory)*

### Functional Requirements

**Identity & Access**

- **FR-001**: System MUST allow a visitor to register with first name, last name, phone, email, password, an optional profile photo, and a **selected currency** chosen from the Admin-managed list of supported currencies.
- **FR-002**: System MUST enforce uniqueness of email and phone across all users.
- **FR-003**: System MUST allow a registered user to log in using either their registered **email** or their registered **phone** together with the password. The system MUST NOT require a separate username field.
- **FR-004**: System MUST assign every newly registered account the **User** role by default; the **Admin** role MUST be assignable only by an existing Admin.
- **FR-005**: System MUST authenticate API calls via JWT bearer tokens issued at login, and MUST authorize actions using **policy-based authorization** keyed on the user's role and ownership of the targeted resource.
- **FR-006**: System MUST hash and salt passwords using an industry-standard algorithm (bcrypt/argon2/PBKDF2-class) and MUST never return password material on any read endpoint.

**Categories**

- **FR-007**: System MUST ship a set of preconfigured Expense categories and a set of preconfigured Income categories, available to all Users in their entry dropdowns.
- **FR-008**: When a category is not listed, the User MUST be able to choose "Not listed Expense?" or "Not listed Income?" and enter a free-text category name that is stored on the entry.
- **FR-009**: Admins MUST be able to create, edit, and delete preconfigured Expense and Income categories; deletion MUST preserve historical entries' displayed category labels.
- **FR-009a**: Admins MUST be able to create, edit, activate, and deactivate **supported currencies** (code, symbol, decimal places) in a global catalog.
- **FR-009b**: Users MUST be able to choose their currency at registration from the active supported-currency list and MUST be able to change it later in profile settings; user-facing amounts and totals MUST be displayed using that user's currency. The MVP MUST NOT perform any FX conversion when a user changes their currency — historical numeric amounts are retained as stored.

**Expense & Income Entries**

- **FR-010**: Users MUST be able to create, read, update, and delete their own Expense and Income entries for any day of the **current** month and any **previous** month.
- **FR-011**: System MUST reject any attempt to create or move an entry to a date in a **future** month (any month after the current month) with a clear error.
- **FR-012**: Each entry MUST capture: type (Expense or Income), amount (positive value, stored using the owning user's selected currency — see FR-009b), date, category (preconfigured reference or free-text label), and an optional note. Decimal precision MUST follow the currency's configured decimal places.
- **FR-013**: A User MUST only be able to view or modify entries they own; cross-user access MUST be denied by policy.

**Monthly View & Totals**

- **FR-014**: The Monthly View MUST display, for the selected month: Total Income (top), Total Expense (top), and Savings = Total Income − Total Expense (bottom).
- **FR-015**: The Monthly View MUST organize entries by day within the selected month and support add/edit/delete inline for allowed months.
- **FR-016**: System MUST allow navigation to any previous month and the current month with full CRUD; navigation to a **future** month MUST render a **read-only** view that shows the projected opening balance (carry-forward) for that month, displays no entries, and exposes no add/edit/delete actions.

**Carry-Forward**

- **FR-017**: System MUST compute a per-month opening balance equal to the closing balance of the previous month, where closing balance = previous opening balance + Total Income − Total Expense.
- **FR-018**: When a User creates, edits, or deletes any entry in any allowed month, the system MUST automatically recompute opening and closing balances for that month and all subsequent months so that all future-month views reflect the change.
- **FR-019**: System MUST display the carry-forward (opening balance) on each monthly view and on the dashboard.

**Dashboard & Alerts**

- **FR-020**: System MUST provide a Dashboard showing an expense graph (at minimum a per-month expense trend) for the signed-in user.
- **FR-021**: System MUST display a carry-forward status indicator using these color bands: **≥ 30% green**, **< 30% and > 20% orange**, **≤ 20% and ≥ 10% orange with red tint**, **< 10% blood red**.
- **FR-022**: System MUST raise a prominent alert when, for the displayed month, Total Expense ≥ Total Income.
- **FR-023**: The carry-forward percentage that drives the dashboard color bands MUST be computed as the **monthly savings rate**: `((Total Income − Total Expense) ÷ Total Income) × 100`, evaluated for the displayed month. When `Total Income = 0`, the percentage is treated as `< 10%` (blood red) per the zero-income edge case in this spec.

**Admin**

- **FR-024**: Admins MUST be able to list all users with pagination/search, view a user, edit user profile fields, and delete (or soft-delete) a user.
- **FR-025**: System MUST prevent the deletion or role-demotion that would leave zero Admins.

**Cross-Cutting**

- **FR-026**: All write operations MUST be transactional so partial updates do not leave totals inconsistent across the carry-forward chain.
- **FR-027**: All API endpoints MUST default to authenticated; anonymous access MUST be limited to registration, login, and explicitly public health endpoints.
- **FR-028**: System MUST validate all user-supplied input on the server (amount > 0 and within the selected currency's decimal precision, date within allowed range, category exists or free text within length limits, profile photo MIME type in {`image/jpeg`, `image/png`}, file size ≤ **2 MB**, dimensions ≤ **2048×2048**).

### Key Entities

- **User** — represents an account holder. Attributes: first name, last name, phone (unique), email (unique), password (hashed), profile photo (optional), role (User or Admin), **selected currency** (FK to Currency), timezone, audit timestamps.
- **Role** — enumerated value: `User` or `Admin`.
- **Category** — preconfigured expense or income category. Attributes: name, type (Expense or Income), active flag. Owned/managed by Admin.
- **Currency** — supported currency in the global catalog. Attributes: ISO code (unique), symbol, decimal places, active flag. Managed by Admin.
- **Entry (Transaction)** — a single expense or income record. Attributes: owner (User), type (Expense or Income), amount (in the owner's currency at time of entry), date, category reference (nullable when free-text), free-text category label (nullable when referenced), optional note, audit timestamps.
- **MonthlySummary (derived)** — for a (User, year, month): opening balance, total income, total expense, closing balance, carry-forward percentage, status color. Computed from Entries; may be cached for read performance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can complete registration and reach the first Monthly View in **under 2 minutes** end-to-end.
- **SC-002**: Adding, editing, or deleting an entry updates the displayed monthly totals and savings within **1 second** of confirmation.
- **SC-003**: Editing an entry in a past month updates the carry-forward in every subsequent month within **2 seconds**, with **100% consistency** (every later month's opening balance equals the prior month's closing balance).
- **SC-004**: At least **95% of users** in usability testing successfully record their first expense without using the "Not listed" fallback when a matching preconfigured category exists.
- **SC-005**: The dashboard color indicator matches the documented threshold table in **100% of tested cases** across all four bands plus the zero-income edge case.
- **SC-006**: Attempts to add an entry for a future month are rejected in **100%** of attempts via both UI and API.
- **SC-007**: A non-Admin User receives an authorization failure on **100%** of admin-only actions attempted directly against the API.
- **SC-008**: The Monthly View loads in **under 1.5 seconds** for a user with up to 12 months of history and 200 entries per month.

## Assumptions

- The login identifier set is **email or phone** (no separate username); password rules follow industry baseline (minimum 8 chars, mix of letters and digits).
- **Profile photo is optional** at registration; accepted formats are **JPEG and PNG**, maximum file size **2 MB**, maximum dimensions **2048×2048** pixels. Uploads outside these bounds are rejected with a clear validation message; registration may still complete without a photo.
- **Currency**: Admins manage the global catalog of supported currencies (code, symbol, decimal places). Each user selects a currency at registration and may change it in profile settings. The MVP does not perform FX conversion: stored amounts are interpreted in the user's currency at time of entry; if a user later changes their currency, displayed numeric values are not recomputed.
- **Timezone**: month boundaries are computed in the user's stored timezone, defaulting to UTC at registration; users may update it in profile settings.
- **Free-text "Not listed" categories** are stored on the entry as a label only and are **not** promoted into the global category list; only Admins create global categories.
- **Category deletion** is soft (deactivation): the category disappears from selection lists but historical entries continue to display the original label.
- **User deletion** by Admin is **soft-delete** by default: the user can no longer log in but their financial history is retained for audit; hard delete is a separate, explicit administrative action.
- **At least one Admin must always exist**; the system blocks the action that would leave zero Admins.
- **Future months are read-only** in the UI: navigation is permitted, the projected opening balance is displayed, and no entries or actions are shown. Write operations to a future-month date are **rejected** at the API.
- **Carry-forward recomputation** for a single user spans only that user's data and is expected to complete synchronously within the SC-003 budget for typical histories (≤ 24 months).
- **Dashboard graph** for the MVP shows at minimum a per-month expense trend; per-category breakdown is a nice-to-have within scope if it fits the planning budget.
