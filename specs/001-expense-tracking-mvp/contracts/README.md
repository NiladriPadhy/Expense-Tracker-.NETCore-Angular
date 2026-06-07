# API Contracts

This folder contains the public API contracts for the Expense Tracking MVP.

## Files

- `openapi.yaml` — OpenAPI 3.1 specification of the REST API exposed by `ExpenseTracker.Api`.

## Conventions

- **Base URL**: `/api/v1`
- **Auth**: Bearer JWT in `Authorization` header. Acquired via `POST /auth/login`.
- **Error model**: RFC 7807 ProblemDetails (`application/problem+json`). Custom application codes appear in the `code` extension field (e.g. `future_month_write_forbidden`).
- **IDs**: `Guid` (UUIDv4) serialized as canonical hyphenated lower-case strings.
- **Money**: `number` (JSON), expressed in the user's currency, with up to `Currency.DecimalPlaces` fractional digits. The currency code is implicit in the authenticated user's profile and echoed in responses.
- **Dates**: `date` (`YYYY-MM-DD`) for entry dates; `date-time` ISO-8601 with offset for audit timestamps.
- **Pagination**: `?page=1&pageSize=20` (1-indexed). Responses include `total`, `page`, `pageSize`.
- **Versioning**: Path-segment major version (`/api/v1`). Breaking changes bump the major.
- **Idempotency**: Mutating endpoints accept an optional `Idempotency-Key` header; if present, repeated calls return the same result.

## Authorization Policies (server-side)

| Policy | Applies to |
|---|---|
| `RequireUser` | Authenticated user (any role) |
| `EntryOwner` | The user owns the entry referenced in the route, or is Admin |
| `RequireAdmin` | Admin role only |

## Anonymous endpoints

Only `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /currencies/active`, `GET /health`. Everything else requires JWT.
