$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$infrastructureProject = "backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj"
$apiProject = "backend/UAF/UAF.API/UAF.API.csproj"
$migrationsPath = Join-Path $repoRoot "backend/UAF/UAF.Infrastructure/Persistence/Migrations"

if ([string]::IsNullOrWhiteSpace($env:UAF_DB_CONNECTION_STRING)) {
    throw "Set UAF_DB_CONNECTION_STRING to the intended SQL Server connection string before generating migrations."
}

if (Test-Path $migrationsPath -PathType Container) {
    $existingMigrations = Get-ChildItem -Path $migrationsPath -Filter "*.cs" -File
    if ($existingMigrations.Count -gt 0) {
        throw "Migrations already exist. Create an incremental migration instead of InitialCreate."
    }
}

Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Failed to restore local .NET tools." }

    dotnet ef migrations add InitialCreate --project $infrastructureProject --startup-project $apiProject --context UafDbContext --output-dir Persistence/Migrations
    if ($LASTEXITCODE -ne 0) { throw "Initial migration generation failed." }

    $artifactDirectory = Join-Path $repoRoot "artifacts/database"
    New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
    dotnet ef migrations script --idempotent --project $infrastructureProject --startup-project $apiProject --context UafDbContext --output (Join-Path $artifactDirectory "UafManagement-InitialCreate.sql")
    if ($LASTEXITCODE -ne 0) { throw "Idempotent SQL script generation failed." }

    Write-Host "Migration generated. Review the migration and SQL script, then commit the Migrations directory."
    Write-Host "Do not apply it to production until reviewed and backed up."
}
finally {
    Pop-Location
}
