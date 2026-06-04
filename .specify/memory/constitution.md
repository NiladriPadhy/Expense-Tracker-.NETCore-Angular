<!--
SYNC IMPACT REPORT
==================
Version change: (uninitialized template) → 1.0.0
Bump rationale: Initial ratification of project constitution (MAJOR baseline).

Modified principles: N/A (initial creation)
Added principles:
  - I. Clean Layered Architecture (NON-NEGOTIABLE)
  - II. Pluggable Persistence & Provider Abstraction
  - III. Security-First Middleware Pipeline
  - IV. Pattern-Driven, Documented Code
  - V. Coding Standards & Quality Gates
Added sections:
  - Technology Stack & Standards
  - Development Workflow & Quality Gates
  - Governance
Removed sections: None

Templates requiring updates:
  - ✅ .specify/templates/plan-template.md   (Constitution Check gates align with principles I–V; planners must reference this constitution)
  - ✅ .specify/templates/spec-template.md   (Scope/requirements format compatible)
  - ✅ .specify/templates/tasks-template.md  (Task categories cover architecture, security, testing, docs)
  - ✅ .specify/templates/checklist-template.md (Generic; no change required)
  - ⚠ README.md (not present at repo root; create at project setup to reference this constitution)

Follow-up TODOs:
  - TODO(README): Add a top-level README.md linking to this constitution once project scaffolding lands.
-->

# ExpenseTracker Constitution

## Core Principles

### I. Clean Layered Architecture (NON-NEGOTIABLE)

The solution MUST be organized into clearly separated, independently buildable layers with
unidirectional dependencies pointing inward toward the domain.

Backend (.NET Core 10) layers:
- **Domain Layer** — entities, value objects, domain services, and abstractions (interfaces).
  Contains no framework, persistence, or transport concerns. Reusable across hosts.
- **Application Layer** — use cases, DTOs, validators, and orchestration; depends only on
  Domain abstractions.
- **Infrastructure Layer** — Entity Framework Core implementations, migrations, external
  service adapters, repository implementations.
- **API Layer** — ASP.NET Core controllers/minimal APIs, middleware, DI composition root,
  authentication/authorization wiring.

Frontend (Angular 21) layers (Clean Architecture):
- **Domain** — entities, interfaces, and use-case contracts (framework-agnostic TypeScript).
- **Application** — use cases / interactors orchestrating domain logic.
- **Infrastructure** — HTTP clients, storage adapters, and other external integrations
  implementing domain interfaces.
- **Presentation** — Angular components, feature modules, routing, state management.

Rules:
- Inner layers MUST NOT depend on outer layers. Violations MUST fail code review.
- Each layer MUST live in its own project/module with explicit dependency declarations.
- Cross-layer communication MUST occur only via interfaces defined by the inner layer.

**Rationale**: Layer separation enforces testability, reuse of the domain model, and the
ability to evolve transport, persistence, or UI without rewriting business logic.

### II. Pluggable Persistence & Provider Abstraction

Database connectivity MUST be plug-and-play. The solution MUST support swapping the SQLite
default for any other Entity Framework Core–compatible provider (e.g., SQL Server,
PostgreSQL, MySQL) **without source code changes** outside the composition root.

Mandatory rules:
- Default development database MUST be SQLite, configured via Entity Framework Core.
- Provider selection MUST be driven by configuration (e.g., `Database:Provider` and
  `ConnectionStrings:Default`); the API Layer composition root MUST select the provider at
  startup using a Factory pattern.
- Migrations MUST be authored and committed for every provider supported in production.
  EF Core migrations are mandatory; ad-hoc schema changes are forbidden.
- All data access MUST go through the Repository abstraction defined in the Domain Layer.
  Direct `DbContext` use outside Infrastructure is forbidden.

**Rationale**: Decoupling the persistence provider protects against vendor lock-in, enables
environment parity (SQLite for dev/test, server DB for prod), and keeps domain code pure.

### III. Security-First Middleware Pipeline

The API Layer MUST implement, in this order, a hardened middleware pipeline:

1. **Global exception handling** — converts exceptions to RFC 7807 ProblemDetails; never
   leaks stack traces to clients in non-Development environments.
2. **CORS** — explicit allow-list policy driven by configuration. Wildcards forbidden in
   production.
3. **Rate limiting** — ASP.NET Core rate limiting middleware with per-endpoint and
   per-client policies; defaults MUST be defined and overridable via configuration.
4. **Authentication** — JWT bearer authentication. Tokens MUST be signed with an
   asymmetric key or a strong symmetric secret loaded from a secret store (never source).
5. **Authorization** — policy-based authorization; role/claim policies declared centrally
   and applied via attributes or endpoint filters.

Additional security rules:
- Secrets (JWT keys, connection strings) MUST be sourced from environment variables, user
  secrets, or a secret manager — never committed to source.
- All API endpoints MUST default to authenticated; anonymous access MUST be explicit.
- HTTPS MUST be enforced in non-Development environments.

**Rationale**: A standard, ordered pipeline eliminates whole classes of vulnerabilities and
ensures consistent behavior across endpoints, satisfying OWASP Top 10 baselines.

### IV. Pattern-Driven, Documented Code

Established design patterns MUST be applied where they add clarity or decoupling, and every
non-trivial implementation MUST be documented.

Required patterns (apply where appropriate):
- **Repository** — for all aggregate persistence access.
- **Factory** — for runtime provider/strategy selection (e.g., DB provider, token signer).
- **Adapter** — for wrapping external services and SDKs behind domain interfaces.
- **Singleton** — only for genuinely stateless, thread-safe services registered via DI as
  `Singleton` (never via static mutable state).
- **Strategy / Options / Mediator (CQRS-lite)** — permitted when justified.

Documentation rules:
- Public types and members in Domain and Application layers MUST have XML doc comments
  (.NET) or TSDoc (Angular).
- Each backend project and Angular feature module MUST contain a `README.md` describing
  responsibilities, key abstractions, and extension points.
- API endpoints MUST be documented via OpenAPI/Swagger with examples and error schemas.
- Non-obvious logic MUST include a brief inline comment explaining intent (the "why").

**Rationale**: Patterns communicate intent; documentation preserves knowledge across
contributors and time. Together they reduce onboarding cost and prevent architectural drift.

### V. Coding Standards & Quality Gates

All code MUST conform to enforced, automated coding standards.

Backend (.NET):
- Target framework MUST be .NET 10. Nullable reference types MUST be enabled solution-wide.
- `TreatWarningsAsErrors` MUST be `true` in Release configuration.
- `.editorconfig` MUST define formatting and analyzer rules; `dotnet format` MUST pass in CI.
- Roslyn analyzers (built-in + StyleCop or equivalent) MUST be enabled with no suppressions
  without an inline justification.
- Naming: PascalCase for types/methods, camelCase for locals/parameters, `_camelCase` for
  private fields, `I` prefix for interfaces.

Frontend (Angular 21):
- Strict TypeScript mode MUST be enabled (`"strict": true`).
- ESLint + Prettier MUST be configured and pass in CI; no warnings allowed in PR builds.
- Angular style guide naming and folder conventions MUST be followed.
- Standalone components and the modern Angular control-flow syntax SHOULD be preferred.

Universal:
- Every PR MUST pass build, lint/format, and the full test suite before merge.
- Public contracts (DTOs, API responses, domain interfaces) MUST have unit tests; new
  features SHOULD include integration tests covering at least the happy path and one
  failure path.

**Rationale**: Automated gates make standards objective, prevent style debates, and keep
the codebase consistently readable and maintainable.

## Technology Stack & Standards

The following stack is canonical for this project. Deviations require a constitution
amendment (see Governance).

- **Backend runtime**: .NET 10 (ASP.NET Core Web API)
- **ORM**: Entity Framework Core 10 with code-first migrations
- **Default database**: SQLite (development); pluggable EF Core providers for other
  environments (configuration-driven)
- **AuthN/AuthZ**: JWT bearer tokens; policy-based authorization
- **API docs**: OpenAPI/Swagger
- **Backend testing**: xUnit (unit + integration); WebApplicationFactory for API tests
- **Frontend framework**: Angular 21 (standalone components, modern control flow)
- **Frontend state**: Service-with-Subject or Signals; NgRx permitted only when complexity
  warrants it (justified in plan.md)
- **Frontend testing**: Jest or Karma+Jasmine for unit; Playwright or Cypress for e2e
- **Tooling**: `.editorconfig`, ESLint, Prettier, dotnet format, analyzers, all enforced in CI

Configuration is the contract for environment differences. No environment-specific code
branches outside designated configuration consumers.

## Development Workflow & Quality Gates

- **Branching**: feature branches per Spec Kit conventions (`###-feature-name`); merges to
  `main` via pull request only.
- **Reviews**: every PR MUST be reviewed for (a) layer/dependency correctness, (b) security
  middleware compliance, (c) test coverage of new behavior, (d) documentation updates.
- **Tests**: unit tests for domain/application logic; integration tests for repositories and
  API endpoints. New principles or constraints introduced here MUST be reflected in the
  Constitution Check gate of `plan-template.md`.
- **Migrations**: every schema change MUST ship as an EF Core migration in the same PR as
  the code change.
- **CI gates** (blocking): build, lint/format, analyzers, unit tests, integration tests,
  OpenAPI generation. Frontend gates: build, lint, unit tests.
- **Observability**: structured logging (Serilog or built-in `ILogger` with JSON formatter)
  MUST be configured; correlation IDs MUST flow from API request through application logic.

## Governance

This constitution supersedes ad-hoc practices and prior conventions. All specifications,
plans, tasks, code reviews, and merges MUST verify compliance with the principles above.

Amendment procedure:
1. Propose the change as a PR modifying `.specify/memory/constitution.md` with rationale.
2. Update the Sync Impact Report at the top of this file and propagate changes to dependent
   templates (`plan-template.md`, `spec-template.md`, `tasks-template.md`, checklists, and
   any agent guidance files).
3. Bump the version per the policy below; update the `Last Amended` date.
4. Require explicit approval from the project maintainer(s) before merge.

Versioning policy (semantic versioning of governance):
- **MAJOR** — backward-incompatible removal or redefinition of a principle or governance rule.
- **MINOR** — addition of a new principle/section or material expansion of guidance.
- **PATCH** — clarifications, wording, typo fixes, non-semantic refinements.

Compliance review:
- Every plan MUST include a Constitution Check gate referencing the principles above.
- Any complexity or deviation MUST be justified in the plan's Complexity/Deviation section
  with a concrete reason and the simpler alternative considered.
- Runtime/agent guidance files (e.g., `README.md`, `.github/copilot-instructions.md`) MUST
  reference this constitution as the source of truth.

**Version**: 1.0.0 | **Ratified**: 2026-06-04 | **Last Amended**: 2026-06-04
