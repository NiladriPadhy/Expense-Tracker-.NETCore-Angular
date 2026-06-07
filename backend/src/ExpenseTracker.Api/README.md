# ExpenseTracker.Api

ASP.NET Core hosting layer: controllers, middleware, authentication and
authorization wiring, OpenAPI/Swagger, rate limiting, and Serilog setup.

## Endpoints

- `/api/v1/auth/{register,login,refresh,logout}` — anonymous (rate-limited via
  the `auth` policy).
- `/api/v1/me`, `/api/v1/me/photo`, `/api/v1/users/{id}/photo` — current user.
- `/api/v1/entries` (CRUD), `/api/v1/months/{year}/{month}`, `/api/v1/dashboard`,
  `/api/v1/categories`, `/api/v1/currencies/active` — authenticated user.
- `/api/v1/admin/{users,categories,currencies}` — admin only
  (`PolicyNames.RequireAdmin`).
- `/health` — anonymous liveness probe.
- `/swagger` — OpenAPI UI.

## Authorization policies

Defined in `Authorization/PolicyNames.cs`:
- `RequireUser` — authenticated User or Admin.
- `RequireAdmin` — Admin role only.
- `EntryOwner` — authenticated user must own the targeted entry id.

## Run

```sh
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project backend/src/ExpenseTracker.Api \
    --no-launch-profile --urls http://localhost:5266
```

Production deployments must override `Jwt:SigningKey` (env: `Jwt__SigningKey`)
or boot will fail with a clear error.
