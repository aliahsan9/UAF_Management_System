# Phase 7 – Testing and Quality Assurance

## Existing and added coverage

- **60 Unit tests:** xUnit is the test framework. Existing unit suites cover domain rules, validators, calculators, services, refresh tokens and middleware.
- **61 Integration tests:** `WebApplicationFactory<Program>` exercises the real ASP.NET Core pipeline, health, authorization middleware, security headers and OpenAPI contract.
- **62 Authorization tests:** role-to-permission matrix tests and endpoint-level anonymous access regressions.
- **63 API contract tests:** the development OpenAPI document is retrieved and checked for key public routes. Add schema snapshots/consumer-driven contracts as clients stabilize.
- **64 SQL Server integration tests:** the repository already contains SQL Server-backed workflow, attendance, fees, grade revision, timetable and transcript tests. CI provisions SQL Server and passes `UAF_TEST_SQLSERVER_CONNECTION_STRING`; each test creates and drops an isolated database. Never point this variable at production.
- **65 Coverage:** Coverlet MSBuild produces Cobertura line/branch coverage. CI uploads TRX and Cobertura reports. Review actual coverage before setting a quality gate; avoid arbitrary thresholds that hide untested modules.
- **66 Architecture tests:** compile-time assembly references enforce Domain → no outer layer, Application → no Infrastructure/API, Infrastructure → no API.
- **67 Performance tests:** k6 smoke/load profile at `tests/performance/k6-student-read.js`. Run against a non-production environment with a least-privilege test token.
- **68 Security tests:** regression tests check anonymous access, safe error output, and invalid verification signatures. Complete OWASP API Security Top 10 testing with authenticated role fixtures, object-level authorization matrices, upload fuzzing, rate-limit tests and dependency scanning before release.
- **69 Concurrency tests:** existing SQL Server admission workflow integration test races decisions and student conversion, asserting one decision winner and one idempotent student registration. Add races for every financial/seat allocation write path.
- **70 End-to-end workflows:** the SQL Server admission integration test exercises submit/review/accept/registration conversion and verifies audit history. Extend with fees→payment→receipt, enrollment→attendance→grades→transcript and HR approval workflows.

## Running locally

```powershell
cd backend/UAF
dotnet restore UAF.sln
dotnet test UAF.sln --configuration Release
```

SQL Server tests are skipped unless `UAF_TEST_SQLSERVER_CONNECTION_STRING` is configured. Use a disposable local SQL Server database user with permission to create/drop databases, never production credentials.

Collect coverage:

```powershell
dotnet test UAF.sln --configuration Release -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura
```

Performance test:

```powershell
k6 run -e BASE_URL=https://test.example.edu -e BEARER_TOKEN=<short-lived-test-token> tests/performance/k6-student-read.js
```

## Release criteria

1. Release build succeeds with no compiler warnings promoted by CI.
2. Unit, API integration, authorization and SQL Server integration suites pass.
3. Review line and branch coverage by module; set ratcheting thresholds after a trusted baseline exists.
4. No high/critical dependency vulnerabilities.
5. k6 p95 and error-rate thresholds pass in a representative non-production environment.
6. Security review includes OWASP API1 object-level authorization and API5 function-level authorization for every sensitive endpoint.
7. End-to-end workflow tests prove atomicity, authorization, auditability and duplicate-request behavior.
