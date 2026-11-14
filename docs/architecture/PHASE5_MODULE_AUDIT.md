# Phase 5 University Management Module Audit

Audit baseline: the repository `master` branch at the time this document was created. This audit distinguishes implemented API workflows from domain concepts mentioned only in the README. A class or DbSet alone is not counted as a working module.

## Findings

| Priority | Module | Finding | Evidence / missing work |
|---:|---|---|---|
| 1 | University & Campus Management | Implemented foundation | University and campus CRUD/deactivation endpoints, application services and persistence exist. Confirm institution-specific multi-university tenancy and campus-scoped permissions before production. |
| 2 | Faculties, Departments & Institutes | Partially implemented | Faculty and department hierarchy is implemented. A distinct Institute entity/workflow and cross-campus ownership rules are not present. |
| 3 | Academic Programs & Degrees | Partially implemented | Academic programs exist. Degree-award/catalogue rules, accreditation metadata, program versions and curriculum linkage need a dedicated workflow. |
| 4 | Academic Sessions & Semesters | Partially implemented | Academic-session lifecycle exists. Semester/term is not a separate entity with date windows, registration windows and session-specific policy. |
| 5 | Admissions & Applications | Implemented substantial workflow | Applicant self-service, documents, review, decisions, audit and conversion exist. Production malware scanning, object storage and live SQL concurrency tests remain rollout gaps. |
| 6 | Student Management | Implemented administrative foundation | Student CRUD/profile/status and academic-program assignment exist. Student self-service/identity ownership and broader student lifecycle services need completion. |
| 7 | Enrollment & Registration | Partially implemented | Course offering enrollment, capacity controls and lifecycle exist. University registration, add/drop windows, prerequisite checks, holds and student self-service registration are not complete. |
| 8 | Fee Management & Scholarships | Partially implemented | Fee structures, accounts, challans, payments and receipts exist. Scholarship awards, eligibility, budgets, approval, disbursement and fee-waiver integration are absent. |
| 9 | Course & Curriculum Management | Partially implemented | Course catalogue and session-specific offerings exist. Program curriculum versions, course requirements, prerequisites, equivalencies and study plans are absent. |
| 10 | Timetable Management | Initial foundation implemented on Phase 5 branch | Teaching-room inventory and recurring weekly timetable entries are implemented with serializable room/instructor/offering/enrolled-student conflict checks. Remaining: teacher availability calendars, one-off events, holidays, exam seating, timezone conversion and SQL Server concurrency integration tests. |
| 11 | Attendance Management | Implemented foundation | Session attendance, correction audit, policies and shortage reporting exist. Validate policy configuration and end-to-end SQL Server behavior before production. |
| 12 | Examination & Results | Implemented foundation | Examination scheduling, marks/results, approval, grading, appeals and revisions exist. Exam hall/seat planning and moderation workflows are not included. |
| 13 | Transcript & Degree Management | Partially implemented | Immutable transcript snapshots, PDF and public verification exist. Degree eligibility/audit, award approval, certificate issuance and revocation are absent. |
| 14 | Faculty & Employee Management | Implemented foundation | Employee records, department transfer/status and teacher profiles exist. Recruitment, contracts, leave, performance and workload workflows are not present. |
| 15 | Payroll & HR | Missing / partial | Employee directory is present, but payroll periods, salary components, deductions, payslips, tax/statutory rules and payroll approvals are absent. |
| 16 | Library Management | Missing | No library catalogue, copies, circulation, reservations, fines or inventory workflow/API was found. |
| 17 | Hostel Management | Missing | No hostel, room/bed inventory, allocation, occupancy, check-in/out or hostel fee workflow/API was found. |
| 18 | Transport Management | Missing | No route, stop, vehicle, capacity, assignment, pass or transport fee workflow/API was found. |
| 19 | Notifications & Announcements | Missing | No notification inbox, announcement publishing, audience targeting, delivery provider or read/acknowledgement workflow was found. |
| 20 | Reports & Analytics | Partially implemented | Attendance reporting/shortage reports exist. There is no central reporting module, role-scoped dashboards, finance/admissions/enrollment KPIs or export workflow. |

## Cross-cutting observations

- The existing code is a modular monolith with Domain, Application, Infrastructure, API and Tests projects. Existing feature folders and endpoint groups should be extended rather than introducing microservices.
- Existing endpoints are mapped centrally in `UAF.API/Program.cs`; feature services/repositories are registered through the Application and Infrastructure dependency-injection extensions.
- Several existing modules have meaningful business logic and tests; however, README feature descriptions do not substitute for SQL Server integration tests, migration validation, or verified end-to-end behavior.
- Existing course offerings already reference academic sessions, courses and employee instructors. Timetable entries should reference those aggregates and enforce room, instructor and cohort conflict rules in the database transaction.
- Existing fee/payment data must not be altered by adding scholarships or payroll. New modules should reference existing student/employee identities and preserve financial audit history.
- No library, hostel, transport, notification, payroll, scholarship, timetable or central reporting feature should be described as complete until its API, application rules, persistence constraints, authorization and tests are present.

## Phase 5 implementation order

1. Complete academic foundations: institute/campus ownership, semester, program curriculum versions and prerequisites.
2. Extend timetable scheduling with teacher availability, holidays, exam seating and schedule change notifications.
3. Add scholarship awards integrated with fee accounts and approvals.
4. Add employee HR and payroll with immutable pay-run snapshots and approvals.
5. Add library circulation, hostel allocation and transport routing/capacity.
6. Add notification inbox/outbox with retryable delivery and audience authorization.
7. Add central reports and analytics with pagination, date boundaries, permission-scoped aggregates and export limits.

## Release gate

Each module is ready for review only when it has:
- Domain entities/value objects with enforced invariants.
- Application contracts, validation and use cases.
- EF Core configuration, foreign keys, indexes and concurrency/transaction controls where needed.
- Versioned API endpoints with permission policies.
- Unit tests and SQL Server integration tests for critical constraints and concurrent operations.
- API documentation and operational notes.
- A reviewed EF Core migration and CI passing on the proposed branch.
