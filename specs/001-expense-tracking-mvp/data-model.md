# Phase 1 Data Model: Expense Tracking MVP

**Date**: 2026-06-04
**Feature**: 001-expense-tracking-mvp

This document defines the persistent data model and the in-memory derived model used by the carry-forward chain. It is provider-agnostic (SQLite default; portable to SQL Server / PostgreSQL / MySQL) and uses EF Core 10 conventions. Decimals use `decimal(18, 4)` to allow currency precision headroom; UI formats per `Currency.DecimalPlaces`.

---

## Entities

### `User`

Represents an account holder.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | PK | |
| `FirstName` | `string(50)` | NOT NULL | |
| `LastName` | `string(50)` | NOT NULL | |
| `Email` | `string(254)` | NOT NULL, **UNIQUE**, case-insensitive | Stored lowercased |
| `PhoneNumber` | `string(20)` | NOT NULL, **UNIQUE** | E.164 format recommended |
| `PasswordHash` | `string(100)` | NOT NULL | BCrypt hash (cost 12) |
| `Role` | `enum: User \| Admin` | NOT NULL, default `User` | FR-004 |
| `CurrencyCode` | `string(3)` | NOT NULL, FK → `Currency.Code` ON DELETE RESTRICT | FR-009b |
| `Timezone` | `string(64)` | NOT NULL, default `UTC` | IANA tz |
| `IsDeleted` | `bool` | NOT NULL, default `false` | Soft delete |
| `CreatedAt` | `DateTimeOffset` | NOT NULL | UTC |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL | UTC |

**Validation**:
- `Email` ≤ 254 chars and matches RFC 5322 lite regex; uniqueness enforced.
- `PhoneNumber` digits + optional leading `+`; uniqueness enforced.
- `Role` change requires Admin policy; FR-025 prevents demoting/deleting the last Admin.

**Relations**:
- 1—0..1 `UserProfilePhoto` (lazy-loaded only via dedicated endpoint).
- 1—N `Entry`.
- 1—N `MonthlySummary`.

---

### `UserProfilePhoto`

Stored separately so user lists never load image bytes.

| Field | Type | Constraints |
|---|---|---|
| `UserId` | `Guid` | PK, FK → `User.Id` ON DELETE CASCADE |
| `ContentType` | `string(64)` | NOT NULL, in {`image/jpeg`, `image/png`} |
| `Bytes` | `byte[]` | NOT NULL, ≤ 2 MB |
| `Width` | `int` | NOT NULL, ≤ 2048 |
| `Height` | `int` | NOT NULL, ≤ 2048 |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL |

---

### `Currency`

Admin-managed catalog (FR-009a).

| Field | Type | Constraints |
|---|---|---|
| `Code` | `string(3)` | PK, ISO 4217 (uppercased) |
| `Symbol` | `string(8)` | NOT NULL |
| `DecimalPlaces` | `int` | NOT NULL, range [0..4] |
| `IsActive` | `bool` | NOT NULL, default `true` |
| `CreatedAt` | `DateTimeOffset` | NOT NULL |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL |

**Seed**: `INR, USD, EUR, GBP, JPY` (see research R-014).

**State transitions**: `Active ⇄ Inactive`. Inactive currencies are not listed in registration / profile selectors but remain referenced by existing users (FR-009b).

---

### `Category`

Admin-managed expense / income category (FR-007, FR-009).

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK |
| `Name` | `string(50)` | NOT NULL, UNIQUE within `Type` |
| `Type` | `enum: Expense \| Income` | NOT NULL |
| `IsActive` | `bool` | NOT NULL, default `true` |
| `CreatedAt` | `DateTimeOffset` | NOT NULL |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL |

**Soft-delete behavior**: Setting `IsActive = false` removes from selection lists; entries that referenced this category retain a denormalized snapshot in `Entry.CategoryNameSnapshot` (see below).

---

### `Entry` (Transaction)

A single income or expense record (FR-010, FR-012).

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | NOT NULL, FK → `User.Id` ON DELETE CASCADE |
| `Type` | `enum: Expense \| Income` | NOT NULL |
| `Amount` | `decimal(18, 4)` | NOT NULL, **> 0** |
| `Date` | `date` | NOT NULL | day-of-month resolution; no time |
| `CategoryId` | `Guid?` | NULL when free-text; FK → `Category.Id` ON DELETE SET NULL |
| `CategoryNameSnapshot` | `string(50)?` | snapshot of the linked category name at write time, or the **free-text label** when `CategoryId` is NULL (FR-008) |
| `Note` | `string(500)?` | optional |
| `CreatedAt` | `DateTimeOffset` | NOT NULL |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL |

**Invariants**:
- Exactly one of (`CategoryId`, `CategoryNameSnapshot` as free text) MUST be set; both populated is allowed when `CategoryId` is the link and `CategoryNameSnapshot` mirrors the name at the time of save.
- `Type` of the linked Category MUST equal `Entry.Type` when `CategoryId` is set.
- `Date` MUST NOT be in a month later than the user's current month, evaluated in `User.Timezone` at write time (FR-011).
- `Amount` MUST be representable to `Currency.DecimalPlaces` of the owner's `User.CurrencyCode`.

**Indexes**:
- `(UserId, Date DESC)` — for monthly view queries.
- `(UserId, Date, Type)` — for monthly aggregations.

---

### `MonthlySummary` (cached derived)

One row per `(UserId, Year, Month)` (R-004).

| Field | Type | Constraints |
|---|---|---|
| `UserId` | `Guid` | PK part 1, FK → `User.Id` ON DELETE CASCADE |
| `Year` | `int` | PK part 2 |
| `Month` | `int` | PK part 3, range [1..12] |
| `OpeningBalance` | `decimal(18, 4)` | NOT NULL, default 0 |
| `TotalIncome` | `decimal(18, 4)` | NOT NULL, default 0 |
| `TotalExpense` | `decimal(18, 4)` | NOT NULL, default 0 |
| `ClosingBalance` | `decimal(18, 4)` | NOT NULL, default 0 |
| `SavingsRatePct` | `decimal(7, 4)?` | NULL when `TotalIncome = 0` |
| `StatusColor` | `enum` | NOT NULL, in {`Green`, `Orange`, `OrangeRedTint`, `BloodRed`} |
| `RecomputedAt` | `DateTimeOffset` | NOT NULL |

**Invariants**:
- `ClosingBalance = OpeningBalance + TotalIncome − TotalExpense`
- For consecutive months `m` and `m+1`: `m+1.OpeningBalance == m.ClosingBalance`.

---

### `RefreshToken`

For JWT refresh-token rotation (R-002).

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | NOT NULL, FK → `User.Id` ON DELETE CASCADE |
| `TokenHash` | `string(100)` | NOT NULL, UNIQUE | SHA-256 of opaque token |
| `IssuedAt` | `DateTimeOffset` | NOT NULL |
| `ExpiresAt` | `DateTimeOffset` | NOT NULL |
| `RevokedAt` | `DateTimeOffset?` | NULL until revoked |
| `ReplacedByTokenHash` | `string(100)?` | rotation chain |

---

## Relationships (ER summary)

```
Currency (1) ──< (N) User (1) ──< (N) Entry (N) >── (0..1) Category
                       │
                       ├──< (N) MonthlySummary
                       ├── (0..1) UserProfilePhoto
                       └──< (N) RefreshToken
```

---

## State Transitions

### `User.Role`

```
User ──(admin promotes)──> Admin
Admin ──(admin demotes; not last admin)──> User
```

### `User` lifecycle

```
Active ──(admin soft-delete)──> SoftDeleted
SoftDeleted ──(admin restore)──> Active
SoftDeleted ──(admin hard-delete)──> Removed
```

Constraint: at all times `count(Active && Role=Admin) >= 1` (FR-025).

### `Category.IsActive` & `Currency.IsActive`

```
Active ──(admin)──> Inactive
Inactive ──(admin)──> Active
```

Inactive entities remain referenced by historical data; only filtered out of selection lists.

---

## Derived / Computed Rules

### Carry-Forward Recompute

Triggered by any insert/update/delete of an `Entry`:

1. Identify earliest affected `(UserId, Year, Month)` (the entry's month, or both old + new for moves).
2. For each month from the earliest affected month forward to the user's `MaxRecordedMonth + 1`:
   1. Recompute `TotalIncome`, `TotalExpense` from entries.
   2. `OpeningBalance := previousMonth.ClosingBalance` (0 if no previous month exists).
   3. `ClosingBalance := OpeningBalance + TotalIncome − TotalExpense`.
   4. `SavingsRatePct := TotalIncome > 0 ? (TotalIncome − TotalExpense) / TotalIncome × 100 : NULL`.
   5. `StatusColor := classify(SavingsRatePct, TotalIncome)` (see research R-005).
3. Persist all updated rows in the same DB transaction.

### Future-Month Projection (no row written)

For requested `(UserId, Year, Month)` later than the latest recorded month: return synthesized response with `OpeningBalance = lastRecorded.ClosingBalance`, all totals zero, `Entries = []`, `ReadOnly = true`.

---

## Indexing Summary

| Table | Index | Reason |
|---|---|---|
| `User` | UNIQUE `Email`, UNIQUE `PhoneNumber` | login lookup, uniqueness |
| `Entry` | `(UserId, Date DESC)` | monthly view |
| `Entry` | `(UserId, Date, Type)` | aggregations |
| `MonthlySummary` | composite PK `(UserId, Year, Month)` | direct read |
| `Category` | UNIQUE `(Name, Type)` | per-type uniqueness |
| `Currency` | PK `Code` | direct lookup |
| `RefreshToken` | UNIQUE `TokenHash`, INDEX `(UserId, ExpiresAt)` | rotation + cleanup |

---

## Migration Notes

- Initial migration `0001_Initial` creates all tables and seeds Currencies + Categories + the bootstrap Admin user (R-014).
- Each provider folder (`Sqlite`, `SqlServer`, `PostgreSQL`, `MySQL`) holds its own migration history; the snapshot `AppDbContextModelSnapshot.cs` is shared.
- `MonthlySummary` rows are populated lazily on first read after the migration; existing entries (none in MVP launch) trigger a one-time backfill.
