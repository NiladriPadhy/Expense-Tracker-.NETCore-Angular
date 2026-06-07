# Frontend `src/app` — Clean Architecture layout

Angular 22 standalone components + Signals. The same dependency-direction rule
as the backend applies: `presentation → infrastructure → application → domain`,
and inner layers must never import outer ones.

## Layers

- `domain/` — TypeScript-only types (no Angular, no RxJS subjects). Models in
  `domain/models/index.ts` mirror backend DTO shapes; abstract `Repository`
  classes live in `domain/ports/`.
- `application/` — use cases and feature state. Plain classes; depend only on
  domain ports. State files (`*.state.ts`) wrap Angular Signals.
- `infrastructure/` — concrete repository implementations
  (`http-*.repository.ts`) using `HttpClient`. The DI providers in
  `app.config.ts` wire ports to these implementations.
- `presentation/` — Angular components, route configs, guards, pipes, and
  cross-cutting UI utilities (`presentation/shared/`).

## Wire format reminders

- All HTTP traffic is JSON camelCase (POST register also accepts multipart-form
  for profile photo upload).
- Backend serializes enums as strings; do not assume integers.
- `MonthlyView.entries` is the only way to fetch a month's entries — there is
  no `GET /entries?year=&month=`.

## Run

```sh
cd frontend
npm start
# http://127.0.0.1:4200
```

`API_CONFIG` defaults to `http://localhost:5142/api/v1`; override in `app.config.ts`
or via environment files for non-default hosts.
