$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$infrastructureProject = "backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj"
$apiProject = "backend/UAF/UAF.API/UAF.API.csproj"
$migrationsPath = Join-Path $repoRoot "backend/UAF/UAF.Infrastructure/Persistence/Migrations"

if ([string]::IsNullOrWhiteSpace($env:UAF_DB_CONNECTION_STRING)) {
    throw "Set UAF_DB_CONNECTION_STRING to the target SQL Server connection string."
}
if (-not (Test-Path $migrationsPath -PathType Container) -or
    -not (Get-ChildItem -Path $migrationsPath -Filter "*.cs" -File -ErrorAction SilentlyContinue)) {
    throw "No committed EF Core migrations were found. Generate, review and commit InitialCreate first."
}

Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Failed to restore local .NET tools." }

    dotnet ef migrations list --project $infrastructureProject --startup-project $apiProject --context UafDbContext
    if ($LASTEXITCODE -ne 0) { throw "Unable to inspect the migration history." }

    dotnet ef database update --project $infrastructureProject --startup-project $apiProject --context UafDbContext
    if ($LASTEXITCODE -ne 0) { throw "Database migration failed. Review the SQL Server error before retrying." }

    Write-Host "EF Core migrations applied successfully."
}
finally {
    Pop-Location
}
