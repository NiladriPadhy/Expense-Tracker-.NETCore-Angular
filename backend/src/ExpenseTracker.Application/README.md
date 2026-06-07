# ExpenseTracker.Application

The orchestration layer. Defines use-case handlers, DTOs, and validation; depends
only on `ExpenseTracker.Domain`.

## Contents

- `Auth/` — `RegisterUserHandler`, `LoginUserHandler`, `RefreshTokenHandler`,
  `LogoutHandler` + DTOs.
- `Entries/` — `Create`/`Update`/`Delete`/`ListEntriesByMonth` handlers.
- `Months/` — `GetMonthlyViewHandler`.
- `Dashboard/` — `GetDashboardHandler`.
- `Admin/Users|Categories|Currencies/` — admin CRUD handlers.
- `Common/` — cross-cutting: `IUserWriteCoordinator`, `JwtOptions`,
  `PolicyNames`, `IUserContext`, `Result`.

## Rules

- No EF Core types; persistence is reached only through `Domain.Abstractions`.
- Handlers are thin: validate, dispatch domain logic, persist via `IUnitOfWork`.
- Validation: FluentValidation validators live alongside handlers.
- Multi-step writes go through `IUserWriteCoordinator` to serialize per user.
