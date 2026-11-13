# UAF Management System — Modular Monolith Architecture

## Architectural decision

UAF is a **modular monolith**, not a collection of independently deployable microservices. The API is deployed as one application, while business capabilities are organized into explicit modules with controlled dependencies.

The current solution keeps four technical projects:

- **UAF.Domain** — enterprise and domain business rules.
- **UAF.Application** — use cases, module contracts, validation and orchestration.
- **UAF.Infrastructure** — persistence, Identity, file storage, PDF generation and external adapters.
- **UAF.API** — HTTP endpoints, authentication/authorization composition, middleware and hosting.

## Dependency direction

Dependencies point inward:

`Domain <- Application <- Infrastructure`

The API is the composition root and references Application and Infrastructure. Domain must not reference any outer layer; Application must not reference Infrastructure or API; Infrastructure must not reference API.

These rules are checked by `UAF.Tests/Architecture/LayerDependencyTests.cs`.

## Module boundaries

Application and API capabilities are grouped by business capability (for example Admissions, Students, CourseOfferings, Fees, Employees, Attendance, Examinations, Grading and Transcripts).

Each module should own:

- Its use cases and application services.
- Request/response models and validators.
- Module-specific repository or gateway contracts.
- Domain entities and policies where the capability owns them.
- API endpoint mapping for its HTTP surface.
- Unit, authorization and integration tests.

Shared abstractions belong in `Common` only when they are genuinely cross-cutting. Avoid a generic repository, service locator, or shared DTO project that erases module ownership.

## Rules for ongoing refactoring

1. Keep endpoint handlers thin: bind/validate HTTP input, call an application use case, map the result.
2. Keep EF Core and SQL-specific behavior inside Infrastructure.
3. Keep authorization decisions close to the use case and enforce them at the API boundary as well.
4. Do not let one feature query another feature's DbSet directly. Expose a narrow application contract or domain operation instead.
5. Keep transaction boundaries explicit in the use case/repository that owns the invariant.
6. Prefer cohesive feature folders and descriptive names over arbitrary folder proliferation.
7. Delete a file only after confirming it has no references, runtime registration, reflection use, migration/design-time use, CI use, or documentation purpose.
8. Preserve public API contracts and database schema unless a separately reviewed migration explicitly changes them.

## Refactoring sequence

1. Establish architecture tests and document the current boundaries.
2. Inventory registrations, endpoint mappings, project references and file usage.
3. Extract feature-specific dependency registration and endpoint mapping from central composition roots.
4. Consolidate duplicate/shared abstractions only after call-site analysis.
5. Remove only proven dead code and generated/local artifacts that are tracked accidentally.
6. Run build, unit tests, SQL Server integration tests, coverage and API contract checks after each slice.

This document describes the target constraints and staged approach; it does not claim that all feature-level boundaries have already been enforced.
