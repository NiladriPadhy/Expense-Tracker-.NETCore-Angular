# ExpenseTracker.Domain

The innermost layer of the backend Clean Architecture. Pure C# with no
external dependencies (no EF Core, no ASP.NET, no logging frameworks).

## Contents

- `Common/` — shared primitives (`Result`, `MonthYear`, `StatusColor`, `Error`).
- `Entities/` — aggregates (`User`, `Currency`, `Category`, `Entry`,
  `MonthlySummary`, `RefreshToken`, `UserPhoto`).
- `ValueObjects/` — `EmailAddress`, `PhoneNumber` (E.164 enforced).
- `Services/` — pure domain services: `MonthlySummaryService`,
  `CarryForwardCalculator`, `SavingsRateClassifier`.
- `Abstractions/` — repository interfaces and `IClock`, `IPasswordHasher`,
  `IUnitOfWork`.

## Rules

- This project must not reference any other project except the BCL.
- Domain logic that mutates state lives on entities/services; orchestration
  belongs in Application.
- All money values are `decimal` and currency-tagged.
