# OpenAPI contract drift report

This file documents differences between
`specs/001-expense-tracking-mvp/contracts/openapi.yaml` (the design contract) and
`/swagger/v1/swagger.json` emitted by the running API. Captured on **2026-06-05**.

The runtime swagger is the authoritative source for clients; the contract here
is kept as a higher-level design artefact. Differences below are **intentional**
implementation choices made during MVP build-out.

## Path drift

| Contract                              | Runtime                              | Notes                                        |
|---------------------------------------|--------------------------------------|----------------------------------------------|
| (n/a)                                 | `/api/v1/auth/register-json`         | JSON helper for SPA; multipart `/register` is the canonical endpoint. |
| `/admin/categories/{categoryId}`      | `/api/v1/admin/categories/{id}`      | Cosmetic param rename; behaviour unchanged.  |
| `/admin/currencies/{code}`            | `/api/v1/admin/currencies/{code}`    | Match.                                       |
| `/admin/users/{userId}`               | `/api/v1/admin/users/{id}`           | Cosmetic param rename.                       |
| `/entries/{entryId}`                  | `/api/v1/entries/{id}`               | Cosmetic param rename.                       |

All other paths match. The contract uses `servers: [/api/v1]`, so contract paths
are unprefixed by design.

## Schema-name drift

The runtime applies a consistent `*Dto` (request bodies and admin DTOs) and
`*Response` (read DTOs) suffix to schemas. The contract uses unsuffixed names.

| Contract            | Runtime                |
|---------------------|------------------------|
| `AdminUserUpdate`   | `AdminUserUpdateDto`   |
| `Category`          | `CategoryResponse`     |
| `CategoryCreate`    | `CreateCategoryDto`    |
| `CategoryUpdate`    | `UpdateCategoryDto`    |
| `Currency`          | `CurrencyResponse`     |
| `CurrencyCreate`    | `CreateCurrencyDto`    |
| `CurrencyUpdate`    | `UpdateCurrencyDto`    |
| `Dashboard`         | `DashboardDto`         |
| `DashboardMonthPoint` | `DashboardMonthPointDto` |
| `Entry`             | `EntryDto`             |
| `EntryCreate`       | `EntryCreateDto`       |
| `EntryUpdate`       | `EntryUpdateDto`       |
| `MonthlyView`       | `MonthlyViewDto`       |
| `Problem`           | (built-in `ProblemDetails`; not registered as a named schema) |
| `UserPage`          | `AdminUserDtoPagedResult` (auto-generated wrapper) |
| `UserProfile`       | `UserProfileDto`       |
| (n/a)               | `AdminCategoryDto`, `AdminCurrencyDto`, `AdminUserDto` |
| (n/a)               | `EntryType`, `StatusColor` (string enum schemas)       |
| (n/a)               | `LoginRequest`, `RefreshRequest`, `RegisterUserRequest`, `UpdateMeDto` |

Field-level wire format is camelCase, matches the contract for shared field
names, and adds string enum serialization for `EntryType`/`UserRole`/`StatusColor`
(via `JsonStringEnumConverter`). See `repo/expense-tracker.md` (memory) for the
canonical field list used by frontend models.

## Resolution

- **No action required for clients**: the frontend Angular models in
  `frontend/src/app/domain/models/index.ts` mirror the runtime DTO shapes.
- **Future work** (optional): regenerate the contract YAML from `swagger.json`
  to keep it in sync with the implementation.
