# UAF University Management System

A university-focused management platform being developed as a Final Year Project (FYP), with the goal of bringing important academic and administrative workflows into one structured digital system.

The project is in its **foundation and early implementation stage**. The repository now contains the initial .NET Domain and Application projects, with core university, student, admission, and course-registration concepts being modelled. The broader university workflows remain under development and must be refined against confirmed requirements.

> **Project status:** In development  
> **Project context:** Independent academic / FYP project  
> **Institutional status:** This is not an official University of Agriculture Faisalabad (UAF) system and is not endorsed by the university unless formally authorized.

---

## Overview

The UAF University Management System is intended to provide a unified platform for managing university-related processes and information. The planned scope includes student-facing services, academic workflows, and administrative operations, with role-appropriate access for students, teachers, and university administrators.

The system is being designed with an emphasis on:

- Clear separation of responsibilities between application modules.
- Secure authentication and role-based authorization.
- Consistent handling of academic and administrative records.
- Maintainable backend architecture.
- A foundation that can be extended as requirements are confirmed.

## Planned Modules

The following areas represent the intended project scope. They are **planned capabilities**, not a statement that these features are already implemented.

| Module | Planned scope |
|---|---|
| User & Role Management | User accounts, authentication, and role-based access for students, teachers, and administrators. |
| Student Management | Student profiles, registration details, and academic records. |
| Admissions & Undergraduate Forms | Online application workflows, form submission, and application status tracking. |
| Academic Management | Faculties, departments, degree programs, academic sessions, courses, and related records. |
| Fee Management | Fee structures, student dues, challans, payment records, and receipts. |
| Identity Card Management | Student and staff identity-card requests, records, and issuance status. |
| Learning Management | A foundation for course-related learning activities and academic resources. |
| Examination & Results | Examination-related records, assessments, and result management. |
| Teacher & Employee Management | Faculty and staff profiles, assignments, and relevant administrative records. |

The final module boundaries and workflows will be refined against documented requirements and feedback from the project supervisor and relevant stakeholders.

## Technology Stack

The backend is planned around the following technologies:

| Area | Planned technology |
|---|---|
| Framework | ASP.NET Core Web API |
| Runtime | .NET 10 |
| Language | C# |
| Architecture | Clean Architecture |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity and JWT |
| Validation | FluentValidation |
| API style | Minimal APIs |
| API documentation | Scalar / OpenAPI |
| Logging | Serilog |
| Testing | xUnit |

> The stack above describes the intended backend direction. Individual technologies will be confirmed in the repository as implementation progresses.

## Backend Architecture

The backend is intended to follow a Clean Architecture structure, separating core domain rules from application workflows, infrastructure concerns, and HTTP delivery.

```text
UAF_Management_System/
└── backend/
    └── UAF/
        ├── UAF.Domain/
        │   ├── Entities/
        │   ├── Enums/
        │   ├── Common/
        │   └── Constants/
        │
        ├── UAF.Application/
        │   ├── Common/
        │   │   └── Abstractions/
        │   └── Features/
        │       └── Admissions/
        │
        ├── UAF.Infrastructure/
        │   ├── Persistence/
        │   ├── Identity/
        │   ├── Repositories/
        │   └── Services/
        │
        ├── UAF.API/
        │   ├── Endpoints/
        │   ├── Middleware/
        │   ├── Extensions/
        │   └── Program.cs
        │
        └── UAF.Tests/
```

The Domain, Application, Infrastructure, and Tests projects have been created. The API project and additional module folders will be added in later stages.

### Layer responsibilities

- **Domain:** Core entities, enums, and business rules. It should not depend on infrastructure or API projects.
- **Application:** Use cases, application services, contracts, DTOs, and validation.
- **Infrastructure:** Database access, Entity Framework Core configurations, Identity integration, and external service implementations.
- **API:** HTTP endpoints, authentication configuration, middleware, dependency injection, and API documentation.
- **Tests:** Unit and integration tests for application behavior and API workflows.

### Dependency direction

```text
UAF.API ───────────────► UAF.Application
   │                           ▲
   │                           │
   ▼                           │
UAF.Infrastructure ────────────┘
   │
   └────────────────────► UAF.Domain

UAF.Application ─────────► UAF.Domain
```

The Domain project remains independent. Infrastructure implements contracts defined by the Application layer, while the API acts as the application entry point.

## Getting Started

The backend currently contains the Domain, Application, Infrastructure, and Tests projects. Infrastructure includes the EF Core SQL Server context, entity configurations, admission repository, dependency registration, and a design-time context factory. The API, database migrations, and production authentication have not yet been implemented. The test suite has been authored but still requires execution in a .NET 10 environment.

### Prerequisites

The planned backend development environment includes:

- Visual Studio 2022 with the .NET 10 SDK.
- SQL Server (local development instance or an appropriately configured server).
- SQL Server Management Studio (SSMS), or another SQL client.
- Git.

### Clone the repository

```bash
git clone https://github.com/aliahsan9/UAF_Management_System.git
cd UAF_Management_System
```

### Backend build and tests

Open `backend/UAF/UAF.sln` in Visual Studio, or run these commands from the repository root:

```bash
dotnet restore backend/UAF/UAF.sln
dotnet build backend/UAF/UAF.sln --no-restore
dotnet test backend/UAF/UAF.sln --no-build
```

The current test project covers the initial admission application workflow using in-memory fakes. The Infrastructure project uses EF Core 10 with SQL Server. Its design-time factory defaults to Windows LocalDB; set the `UAF_DB_CONNECTION_STRING` environment variable to target another development database.

### EF Core migrations

After restoring the solution and ensuring SQL Server LocalDB is installed, install the EF Core CLI tool and create the initial migration:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef migrations add InitialCreate --project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj --startup-project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj --output-dir Persistence/Migrations
dotnet ef database update --project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj --startup-project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj
```

The design-time factory uses `UAF_DB_CONNECTION_STRING` when set; otherwise it targets `(localdb)\\MSSQLLocalDB` and a local database named `UafManagementDb`. These commands are documented but have not yet been executed against a database.

Do not place real database passwords, JWT signing keys, API keys, or other secrets in source control. Use .NET user secrets or environment variables for local development and secure secret storage for deployed environments.

## Security & Engineering Principles

Security and data integrity are important because university systems can process personal, academic, and financial information. The implementation is expected to address:

- Authentication and authorization for protected operations.
- Role-based access control, with permissions enforced by the backend.
- Server-side validation of incoming data.
- Safe handling of credentials and configuration secrets.
- Consistent database constraints and transaction boundaries.
- Auditability for sensitive administrative operations.
- Automated tests for important business rules and access-control behavior.

These are engineering objectives; their implementation and verification will be documented as development progresses.

## Development Roadmap

- [ ] Confirm project requirements and module boundaries.
- [x] Create the initial .NET Domain and Application projects.
- [x] Model the initial university structure, students, admissions, academic sessions, courses, offerings, and course enrollments.
- [x] Establish the first Application-layer admission use cases and persistence contracts.
- [x] Create the .NET solution file and initial Domain, Application, and Tests projects.
- [x] Create the Infrastructure project with EF Core SQL Server integration.
- [x] Configure entity mappings, keys, indexes, relationships, and database constraints.
- [x] Implement the admission application repository and infrastructure dependency registration.
- [ ] Generate and apply the initial EF Core migration against a real development database.
- [ ] Create the API project and HTTP endpoints.
- [ ] Set up ASP.NET Core Identity and JWT authentication.
- [ ] Implement roles and authorization policies.
- [ ] Establish the university structure (faculties, departments, programs, and academic sessions).
- [ ] Develop student and admissions workflows.
- [ ] Develop fee and identity-card workflows.
- [ ] Expand academic, LMS, examination, and employee modules.
- [x] Add initial unit tests for admission application workflows (execution still pending).
- [ ] Add broader domain, integration, and authorization tests.
- [ ] Document deployment, operations, and backup procedures.

Roadmap items will be checked off only when the corresponding work is implemented and verified.

## Project Goals

This project aims to demonstrate practical software engineering through:

- Requirements analysis and domain modelling.
- Layered architecture and maintainable application design.
- Relational database design and data integrity.
- Secure API development.
- Testing and documentation.
- Iterative development based on feedback.

## Contributing

This is an academic project currently under active development. Suggestions, issue reports, and constructive feedback are welcome. Please open an issue to discuss a proposed change before submitting a pull request.

## Disclaimer

This repository contains an independent academic software project. **UAF** is used to identify the university context for which the project is being designed. The project is not an official university product, does not represent university policy, and must not be presented as an authorized university platform without explicit institutional approval.

## Author

**Ali Ahsan**  
GitHub: [@aliahsan9](https://github.com/aliahsan9)

---

<p align="center">
  Built as an academic project with a focus on maintainable architecture, secure workflows, and practical university management.
</p>
