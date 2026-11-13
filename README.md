# UAF University Management System

A university-focused management platform being developed as a Final Year Project (FYP), with the goal of bringing important academic and administrative workflows into one structured digital system.

The project is in its **foundation and early implementation stage**. The repository contains .NET Domain, Application, Infrastructure, API, and Tests projects. Initial academic hierarchy management (universities, campuses, faculties, departments, and programs), administrative student-management use cases and endpoints, admission workflow endpoints, and an initial Identity/JWT foundation are implemented in source. Student self-service, fees, LMS, examinations, and other broader workflows remain under development and must be refined against confirmed requirements.

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
        │       ├── Admissions/\n        │       ├── AcademicStructure/\n        │       └── Students/
        │
        ├── UAF.Infrastructure/
        │   ├── Persistence/
        │   │   ├── Configurations/
        │   │   └── Repositories/
        │   └── DependencyInjection.cs
        │
        ├── UAF.API/
        │   ├── Endpoints/
        │   ├── Middleware/
        │   ├── Extensions/
        │   └── Program.cs
        │
        ├── UAF.Tests/
        │   └── Admissions/
        │
        └── UAF.sln
```

The Domain, Application, Infrastructure, API, and Tests projects have been created. The API exposes the initial admission workflow, administrative academic-structure endpoints, and an initial ASP.NET Core Identity/JWT authentication foundation. Academic-structure operations are restricted to DepartmentAdmin and SuperAdmin roles. Email verification, refresh-token rotation, account recovery, and controlled administrative user provisioning remain future security work.

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

The backend currently contains the Domain, Application, Infrastructure, API, and Tests projects. The API includes versioned admission endpoints, centralized Problem Details exception handling, health checks, and development-only OpenAPI/Scalar documentation. Infrastructure includes the EF Core SQL Server context, entity configurations, admission repository, dependency registration, and a design-time context factory. Database migrations and production authentication have not yet been implemented. The test suite has been authored but still requires execution in a .NET 10 environment.

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

### Authentication configuration

The API uses ASP.NET Core Identity with GUID user and role keys. Public registration always assigns the `Student` role; clients cannot submit a role name or grant themselves elevated privileges. The initial role set is `Student`, `Teacher`, `DepartmentAdmin`, `Registrar`, `FinanceOfficer`, and `SuperAdmin`. Role seeding is opt-in and must be run only after the Identity database migration has been applied.

Configure JWT settings with .NET user secrets (do not commit a signing key):

```bash
dotnet user-secrets init --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:Issuer" "UAF.Management.Api" --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:Audience" "UAF.Management.Client" --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<generate-a-cryptographically-random-secret-of-at-least-32-bytes>" --project backend/UAF/UAF.API/UAF.API.csproj
```

The API refuses to start if issuer, audience, or a signing key of at least 32 bytes is missing. Access tokens currently expire after 15 minutes. After applying migrations, role creation can be enabled for a one-time development startup using the environment variable `Identity__SeedRolesOnStartup=true`; disable it again after roles are seeded. Production role provisioning should be performed through a controlled administrative process.

Academic structure routes (all require DepartmentAdmin or SuperAdmin):\n\n| Method | Route | Purpose |\n|---|---|---|\n| GET / POST | `/api/v1/academic-structure/universities` | List / create university records |\n| PUT | `/api/v1/academic-structure/universities/{id}` | Update university details |\n| GET / POST | `/api/v1/academic-structure/campuses` | List / create campuses (`universityId` query filter supported) |\n| PUT / DELETE | `/api/v1/academic-structure/campuses/{id}` | Update / deactivate a campus |\n| GET / POST | `/api/v1/academic-structure/faculties` | List / create faculties (`universityId` query filter supported) |\n| PUT / DELETE | `/api/v1/academic-structure/faculties/{id}` | Update / deactivate a faculty |\n| GET / POST | `/api/v1/academic-structure/departments` | List / create departments (`facultyId` query filter supported) |\n| PUT / DELETE | `/api/v1/academic-structure/departments/{id}` | Update / deactivate a department |\n| GET / POST | `/api/v1/academic-structure/programs` | List / create academic programs (`departmentId` query filter supported) |\n| PUT / DELETE | `/api/v1/academic-structure/programs/{id}` | Update / deactivate an academic program |\n\nCodes are normalized to uppercase and checked for duplicates within their parent. Database unique indexes remain the final integrity safeguard. Parent records must exist; inactive faculties and departments cannot be used to create children. Deactivation is a soft state change and does not delete historical records.\n\nAcademic session routes (all require Registrar, DepartmentAdmin, or SuperAdmin):

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/academic-sessions?status=Planned` | List sessions, optionally filtered by lifecycle status |
| GET | `/api/v1/academic-sessions/{id}` | Session details |
| POST | `/api/v1/academic-sessions` | Create a planned session |
| PUT | `/api/v1/academic-sessions/{id}` | Update a planned session |
| POST | `/api/v1/academic-sessions/{id}/activate` | Activate a planned session |
| POST | `/api/v1/academic-sessions/{id}/complete` | Complete an active session |
| POST | `/api/v1/academic-sessions/{id}/cancel` | Cancel a planned or active session |

Session codes are unique and normalized to uppercase. End dates must be later than start dates. Active and completed sessions cannot be edited; lifecycle transitions are enforced in the domain.

Course catalogue routes (all require Registrar, DepartmentAdmin, or SuperAdmin):

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/courses?departmentId={id}&isActive=true` | List courses with optional department and active-state filters |
| GET | `/api/v1/courses/{id}` | Course details |
| POST | `/api/v1/courses` | Create a course under an active department |
| PUT | `/api/v1/courses/{id}` | Update course catalogue details |
| DELETE | `/api/v1/courses/{id}` | Soft-deactivate a course |
| POST | `/api/v1/courses/{id}/reactivate` | Reactivate a course |

Course codes are unique within their department, not globally. Credit hours must be greater than zero and no more than 10, matching the existing domain and database constraint. A course cannot be deactivated while it has draft or published offerings; historical offerings are retained. The course catalogue is separate from session-specific course offerings and student enrollment.

Course offering routes (all require Registrar, DepartmentAdmin, or SuperAdmin):

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/course-offerings?academicSessionId={id}&courseId={id}&status=Published` | Filter scheduled course sections |
| GET | `/api/v1/course-offerings/{id}` | Offering details and current enrolled count |
| POST | `/api/v1/course-offerings` | Schedule a course section in a planned or active session |
| PUT | `/api/v1/course-offerings/{id}` | Update capacity and instructor assignment |
| POST | `/api/v1/course-offerings/{id}/publish` | Publish a draft offering for enrollment |
| POST | `/api/v1/course-offerings/{id}/close` | Close a published offering |
| POST | `/api/v1/course-offerings/{id}/cancel` | Cancel a draft or published offering |
| GET | `/api/v1/course-offerings/{id}/enrollments` | List registration history for an offering |
| POST | `/api/v1/course-offerings/{id}/enrollments/{studentId}` | Enroll an active student in a published offering |

Course enrollment lifecycle routes (same staff policy):

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/course-enrollments/{id}/drop` | Drop an active registration |
| POST | `/api/v1/course-enrollments/{id}/withdraw` | Withdraw an active registration |
| POST | `/api/v1/course-enrollments/{id}/complete` | Mark an active registration completed |

For the enrollment endpoint, provide the academic enrollment date explicitly in the request body (ISO date format):

```json
{
  "enrolledOn": "2026-11-13"
}
```

Offering sections are unique per course and academic session. Only active catalogue courses and planned/active sessions can be scheduled. An offering begins as Draft; enrollment is accepted only after publication. Capacity must be positive and cannot be reduced below the active enrollment count. Publishing also requires the course to remain active and its session to remain planned or active. Dropping or withdrawing a registration releases its seat; a student may re-enroll after dropping or withdrawing, while a filtered unique database index prevents duplicate enrolled/completed registrations for the same student and offering.

Enrollment insertion and capacity updates use SQL Server serializable transactions with an update lock on the offering row. The database also enforces unique active/completed enrollment and section constraints. The application unit tests include concurrent enrollment attempts against a synchronized repository test double. These controls are designed to prevent concurrent requests from allocating the same final seat twice; SQL Server integration tests under concurrent load are still required before production use. Instructor IDs are stored as references, but employee existence validation is deferred until the Employee module and its relationship are implemented. Student self-service enrollment is not exposed; these endpoints are staff-only until identity ownership and registration-window rules are designed.

Student management routes (all require Registrar, DepartmentAdmin, or SuperAdmin):

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/students?page=1&pageSize=20` | Paginated student list; optional `academicProgramId` and `status` filters |
| GET | `/api/v1/students/{id}` | Student details |
| POST | `/api/v1/students` | Register a student with an institution-issued registration number |
| PUT | `/api/v1/students/{id}/profile` | Update student name and contact details |
| PUT | `/api/v1/students/{id}/academic-program` | Change academic-program assignment |
| POST | `/api/v1/students/{id}/suspend` | Suspend an active student |
| POST | `/api/v1/students/{id}/reactivate` | Reactivate a suspended student |
| POST | `/api/v1/students/{id}/graduate` | Mark an active or suspended student as graduated |
| POST | `/api/v1/students/{id}/withdraw` | Withdraw an eligible student |

Student records use the existing unique registration-number database index. Registration numbers are supplied by authorized staff rather than generated from an assumed UAF numbering policy. Listing is paginated (maximum 100 records per page) and supports program/status filters. Academic-program assignment requires an active program. Status transitions are enforced by the domain entity; graduated or withdrawn students cannot be moved to another program. Student self-service endpoints are not exposed until a trusted Identity-user-to-student ownership relationship and authorization tests are implemented.

Authentication routes:

| Method | Route | Access |
|---|---|---|
| POST | `/api/v1/auth/register` | Anonymous; creates a Student account |
| POST | `/api/v1/auth/login` | Anonymous |
| GET | `/api/v1/auth/me` | Valid JWT required |

Send access tokens using the `Authorization: Bearer <token>` header.

### Applicant undergraduate application self-service

Authenticated accounts assigned the default `Student` role can use the applicant routes below. The API derives the owner from the authenticated JWT subject; callers cannot submit or override `OwnerUserId`.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/applicant/admissions` | List only the authenticated applicant's applications |
| GET | `/api/v1/applicant/admissions/{id}` | Read an owned application |
| POST | `/api/v1/applicant/admissions` | Create a draft for an active academic program |
| PUT | `/api/v1/applicant/admissions/{id}` | Update applicant details while the application is Draft |
| GET | `/api/v1/applicant/admissions/{id}/submission-readiness` | Check required and missing verified documents |
| POST | `/api/v1/applicant/admissions/{id}/submit` | Submit only when every configured required document is verified |
| POST | `/api/v1/applicant/admissions/{id}/withdraw` | Withdraw an eligible owned application |

Applicant-created application numbers are generated server-side. An application is editable only in Draft status. Ownership-scoped repository queries return not-found for an application that belongs to another account, avoiding IDOR disclosure. Legacy staff-created records can have a null owner and are not visible through applicant self-service until linked through a controlled process.

### Admission document management and verification

Applicant document routes require the authenticated `Student` role and are scoped to the application owner from the JWT.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/applicant/admissions/{applicationId}/documents` | List documents attached to an owned application |
| POST | `/api/v1/applicant/admissions/{applicationId}/documents` | Upload a document to an owned Draft application |
| GET | `/api/v1/applicant/admissions/{applicationId}/documents/{documentId}/download` | Download an owned document |

Upload uses `multipart/form-data` with fields `file` and `documentType`. Supported document types are `IdentityDocument`, `AcademicTranscript`, `Photograph`, and `Other`. The current development policy accepts PDF, JPEG, and PNG files up to 10 MB. The server checks extension, declared content type, and file signature; records a SHA-256 digest; generates an opaque storage key; and never uses the client filename as a filesystem path.

Staff document review routes require the admissions staff policy (Registrar, DepartmentAdmin, or SuperAdmin):

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/admissions/documents/requirements/program/{academicProgramId}` | Read a program's configured required document types |
| PUT | `/api/v1/admissions/documents/requirements/program/{academicProgramId}` | Replace the required document-type checklist for a program |
| GET | `/api/v1/admissions/documents/application/{applicationId}` | List documents for staff review |
| GET | `/api/v1/admissions/documents/{documentId}/download` | Download a document for verification |
| POST | `/api/v1/admissions/documents/{documentId}/verify` | Verify a pending document |
| POST | `/api/v1/admissions/documents/{documentId}/reject` | Reject a pending document with a reason |

Verification stores reviewer Identity ID, UTC review timestamp, status, and remarks. A document can only be reviewed once; a new upload is required after rejection.

Admissions staff configure the program checklist with a request such as:

```json
{
  "requiredDocumentTypes": ["IdentityDocument", "AcademicTranscript", "Photograph"]
}
```

The request replaces the complete checklist for that academic program; duplicate or unknown enum values and an empty checklist are rejected. The configured-by staff account is recorded on each requirement, and a unique database index prevents duplicate document-type rules for one program. Applicants can inspect readiness before submitting. A submission is blocked when no checklist has been configured or when any required type lacks at least one Verified upload. Pending and Rejected uploads do not satisfy a requirement. Both applicant self-service and staff-created application submission use this same gate. The example above is illustrative only and is **not** asserted to be UAF's official checklist.

The Infrastructure layer currently provides a local development storage adapter. Files are stored outside `wwwroot`, with opaque generated names and no static-file route. Configure `AdmissionDocumentStorage:RootPath` to a private directory with restricted filesystem permissions. For production, replace this adapter with private object storage, add malware scanning/quarantine, backup/retention policies, and monitoring. The local provider is not a shared-storage solution for multiple API instances.

**Admission policy note:** the checklist is configurable and intentionally has no assumed UAF defaults. The university's actual required documents, per-program differences, file rules, and deadlines must be confirmed against authorized requirements before production configuration. Submission gating is implemented for both applicant and staff submission routes. Policy changes should be coordinated with the admissions office because they affect every draft application in that program.

### Admission review, decision audit, and student conversion

The staff workflow records the authenticated reviewer and UTC timestamp when a submitted application enters review. Accept, reject, and waitlist decisions are recorded with the authenticated decision-maker, UTC timestamp, previous/new status, and remarks in an append-only application audit table. All final decisions require remarks; rejection and waitlisting should include the specific eligibility or documentation reason. Decision transitions use a serializable SQL Server transaction and an update lock on the application row so concurrent decisions cannot both succeed.

| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/v1/admissions/{id}/start-review` | Registrar, DepartmentAdmin, SuperAdmin |
| POST | `/api/v1/admissions/{id}/accept` | Registrar, SuperAdmin |
| POST | `/api/v1/admissions/{id}/reject` | Registrar, SuperAdmin |
| POST | `/api/v1/admissions/{id}/waitlist` | Registrar, SuperAdmin |
| GET | `/api/v1/admissions/{id}/decision-history` | Registrar, SuperAdmin |
| POST | `/api/v1/admissions/registration-series` | Registrar, SuperAdmin |
| POST | `/api/v1/admissions/{id}/convert-to-student` | Registrar, SuperAdmin |

Before conversion, a Registrar configures a registration-number series for the academic program and application year:

```json
{
  "academicProgramId": "00000000-0000-0000-0000-000000000000",
  "admissionYear": 2026,
  "prefix": "UAF-CS",
  "nextNumber": 1
}
```

The example prefix is illustrative and is not an official UAF registration-number policy. The server allocates the next number (for example, `UAF-CS-2026-00001`) under a row lock. The counter increment, Student insert, applicant Identity link, and registrar/time metadata are saved in one serializable transaction. A unique index on the originating admission application plus an application-row lock makes repeated or concurrent conversion requests idempotent: the same Student record is returned rather than creating a duplicate. A unique Identity-user link prevents one applicant account from being converted into multiple Student records.

The Student record keeps the source AdmissionApplication ID, optional applicant Identity ID, registration number, registrar ID, and UTC registration timestamp. Its enrollment date is the conversion date. The registration series year is derived from the application's application date; confirm this convention with the authorized university registrar before production use.

**Remaining admission hardening:** run the opt-in SQL Server integration test in a dedicated SQL Server environment, expand coverage for checklist-change/submission races, add malware scanning/quarantine, enforce audit retention controls, and replace local document storage with production object storage. Unit, domain, and authorization tests have been authored but have not been executed in this environment.


The opt-in SQL Server integration test creates a uniquely named temporary database, exercises two simultaneous decisions and two simultaneous conversion requests, then verifies that only one Student and one registration number are created. It drops the temporary database afterward. Run it only against a SQL Server instance where the test account is permitted to create and drop databases:

```powershell
$env:UAF_TEST_SQLSERVER_CONNECTION_STRING = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet test backend/UAF/UAF.Tests/UAF.Tests.csproj --filter "Category=SqlServerIntegration"
```

### Admission API access control

Applicant self-service routes require the `Student` role and derive the application owner from the authenticated Identity account. Ownership-scoped queries prevent applicants from reading, editing, uploading to, or withdrawing another applicant's application. Admissions staff routes use backend authorization policies; configuring program-wide document requirements is restricted to Registrar and SuperAdmin, while document review and general admissions operations use the admissions staff policy.

| Operation | Required role |
|---|---|
| Applicant draft, readiness, upload, submit, withdraw | Student (owned applications only) |
| Configure program document checklist | Registrar, SuperAdmin |
| Staff document review and download | Registrar, DepartmentAdmin, SuperAdmin |
| Staff-created application submission and review workflow | Registrar, DepartmentAdmin, SuperAdmin |
| Accept, reject, waitlist, view decision history, configure registration series, convert accepted applicant | Registrar, SuperAdmin |

These requirements are enforced by ASP.NET Core authorization policies and ownership checks in the Application/Infrastructure layers, not by frontend visibility. Endpoint authorization metadata tests have been added; full HTTP authorization and SQL Server integration tests remain necessary.

The current implementation is an initial authentication foundation, not a production-complete identity lifecycle: email verification, refresh-token rotation/revocation, password reset, MFA, audit events, and privileged account provisioning still need implementation and tests. Do not deploy it with real university accounts until those controls and security tests are completed.


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
- [x] Create the API project and initial admission HTTP endpoints.
- [x] Add centralized Problem Details exception handling and health endpoint.
- [x] Add development-only OpenAPI and Scalar documentation.
- [x] Set up the initial ASP.NET Core Identity and JWT authentication foundation.
- [ ] Add email verification, refresh-token rotation/revocation, password reset, and MFA.
- [x] Add admissions and academic-structure authorization policies (role seeding still requires migration and runtime verification).
- [x] Implement university, campus, faculty, department, and academic-program management endpoints with parent validation and unique-code checks.\n- [x] Add academic-session management endpoints and lifecycle tests. (Execution pending.)
- [x] Implement initial administrative student-management workflows.
- [x] Implement academic-session lifecycle and course catalogue management (build/test execution pending).
- [x] Implement course offering scheduling and student enrollment workflows (build/test execution pending).\n- [x] Implement applicant ownership, private document upload/review, configurable program requirements, and verified-document submission gating (automated test execution and database migration pending).
- [x] Add admission decision actor/time audit records and atomic, idempotent acceptance-to-student conversion (SQL Server integration verification pending).
- [ ] Develop fee and identity-card workflows.
- [ ] Expand academic, LMS, examination, and employee modules.
- [x] Add initial unit tests for admission application workflows (execution still pending).\n- [x] Implement administrative student registration, profile maintenance, program assignment, paginated search, and status lifecycle endpoints.\n- [x] Add student-management application unit tests (execution still pending).
- [x] Add admission decision domain/service tests and endpoint/role authorization policy tests (execution pending).
- [x] Add an opt-in SQL Server integration test for concurrent decisions and idempotent student conversion (execution against a dedicated SQL Server instance pending).
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


## Fee and Challan Management

The backend now includes an initial Finance module:

- Configurable fee heads and fee structures by academic program, academic session, and semester.
- Finance Officer / SuperAdmin-only fee configuration, challan issuance, payment recording, verification, and account inquiry.
- Student fee accounts created on first challan issuance.
- Challan line snapshots preserve fee-head code, name, and amount at issuance time.
- PKR amounts use SQL Server `decimal(18,2)`; fee amounts are configurable and are not represented as an official UAF fee schedule.
- Payments are recorded as pending verification. A different Finance Officer account must verify or reject the payment; rejection requires remarks.
- SQL Server serializable transactions and row locks protect payment verification from concurrent overpayment.
- Outstanding balances include only verified payments; pending and rejected payments do not reduce the balance.

### Finance API

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/finance/fee-heads` | Create a configurable fee head |
| POST | `/api/v1/finance/fee-structures` | Configure fee items for program/session/semester |
| POST | `/api/v1/finance/fee-structures/{id}/publish` | Publish a fee structure |
| POST | `/api/v1/finance/challans` | Issue a student challan |
| POST | `/api/v1/finance/challans/{id}/payments` | Record a payment for verification |
| POST | `/api/v1/finance/payments/{id}/verification` | Verify or reject a payment |
| GET | `/api/v1/finance/students/{studentId}/account` | View balances, challans, and payment history |

All routes currently require FinanceOfficer or SuperAdmin. Student self-service fee-account access, payment gateway reconciliation, bank integration, scheduled late-fee calculation, reversal/refund workflows, and immutable payment audit events remain future hardening work. Late fees are currently supplied explicitly by an authorized Finance Officer; do not assume an official UAF late-fee policy.

The module has domain tests authored. No .NET build, test run, migration generation, or database update has been performed in this environment. Create and apply an EF Core migration after reviewing the model and running the full test suite locally.


Run the fee payment concurrency integration test against a dedicated SQL Server instance (the test creates and drops a temporary database; the account needs permission to create and drop databases):

```powershell
$env:UAF_TEST_SQLSERVER_CONNECTION_STRING = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet test backend/UAF/UAF.Tests/UAF.Tests.csproj --filter "Category=SqlServerIntegration"
```
