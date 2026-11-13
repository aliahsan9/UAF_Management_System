# Backend environment and operations configuration

## Environments

The API uses the standard ASP.NET Core configuration precedence:

1. `appsettings.json` (shared, non-secret defaults)
2. `appsettings.{Environment}.json`
3. Environment variables
4. User Secrets (Development only)

Set `ASPNETCORE_ENVIRONMENT` to `Development`, `Staging`, or `Production`.
Never commit credentials, signing keys, or production connection strings.

### Required settings

- `ConnectionStrings__UafDatabase`: SQL Server connection string (required outside the local Development default).
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`: JWT settings. The signing key must contain at least 32 UTF-8 bytes.
- `AdmissionDocumentStorage__RootPath`: private document storage location.

Use User Secrets locally, for example:

```powershell
dotnet user-secrets init --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:Issuer" "UAF-Management-System" --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:Audience" "UAF-Management-System-Clients" --project backend/UAF/UAF.API/UAF.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-byte-local-secret>" --project backend/UAF/UAF.API/UAF.API.csproj
```

Set production values through the deployment platform's secret manager, not JSON files.

## Structured logging

Serilog writes structured events to stdout and rolling daily files under `logs/`.
Files are retained for 14 days. In containers, prefer stdout collection and mount a persistent
volume only if local file retention is required. SQL text and parameter values are deliberately
not logged by the EF Core command interceptor.

## Health endpoints

- `GET /health/live`: process liveness; does not require SQL Server.
- `GET /health/ready`: readiness including SQL Server connectivity.
- `GET /health`: compatibility alias for readiness.

## API documentation and URI versioning

OpenAPI JSON and Scalar are enabled only in Development. Existing endpoints use explicit URI
version segments (for example, `/api/v1/admissions`). Treat v1 contracts as immutable. Introduce
v2 endpoints under `/api/v2` when a breaking contract change is required; do not silently change
the v1 request/response contract. Keep version-specific route groups and OpenAPI descriptions
together when adding a new version.

## EF Core migrations

Migrations are source-controlled artifacts. Do not call `EnsureCreated` or run schema changes
automatically during API startup. Generate migrations using the existing guarded script:

```powershell
$env:UAF_DB_CONNECTION_STRING = "<development-sql-server-connection-string>"
./scripts/database/New-UafInitialMigration.ps1
```

Review the generated C# migration and idempotent SQL under `artifacts/database`, run it against
a disposable database, add it to source control, and only then deploy it. Apply reviewed migrations
using `scripts/database/Apply-UafMigrations.ps1` after taking a backup. Production deployments
should run migrations as a controlled release step, not from every API replica.
