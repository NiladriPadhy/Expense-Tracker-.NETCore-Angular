# ExpenseTracker.Infrastructure

Adapters for external systems. Depends on Domain + Application; never
references the Api project.

## Contents

- `Persistence/AppDbContext.cs` — EF Core context.
- `Persistence/Configurations/` — `IEntityTypeConfiguration<T>` per aggregate.
- `Persistence/Migrations/Sqlite/` — generated EF migrations for the SQLite
  provider. Each pluggable provider gets its own migration set.
- `Persistence/Repositories/` — repository implementations behind `IXxxRepository`.
- `Persistence/UnitOfWork.cs` — `IUnitOfWork` bridge to `DbContext.SaveChanges`.
- `Persistence/Seeding/SeedRunner.cs` — applies migrations and seeds default
  currencies + the bootstrap admin (idempotent).
- `Auth/` — `BcryptPasswordHasher`, `JwtTokenService`.
- `Photos/ImageSharpPhotoProcessor.cs` — re-encodes uploaded profile photos
  to WebP using SixLabors.ImageSharp.

## DB provider switching

`DbContextOptionsFactory` chooses between SQLite/SQL Server/Postgres based on
`Database:Provider` configuration; the connection string is taken from
`ConnectionStrings:Default`.
