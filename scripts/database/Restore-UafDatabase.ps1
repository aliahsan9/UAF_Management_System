[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "High")]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9_]+$')][string]$Database,
    [Parameter(Mandatory = $true)][string]$BackupPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9_]+$')][string]$DataLogicalName,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9_]+$')][string]$LogLogicalName,
    [Parameter(Mandatory = $true)][string]$DataDirectory,
    [Parameter(Mandatory = $true)][string]$LogDirectory,
    [string]$Server = "localhost"
)

$ErrorActionPreference = "Stop"
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd was not found. Install the Microsoft SQL Server command-line tools."
}
if (-not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) {
    throw "Backup file not found: $BackupPath"
}

$fullBackupPath = [System.IO.Path]::GetFullPath($BackupPath)
$escapedBackupPath = $fullBackupPath.Replace("'", "''")
$escapedDatabase = $Database.Replace("]", "]]")
$escapedDatabaseLiteral = $Database.Replace("'", "''")
$escapedDataLogical = $DataLogicalName.Replace("'", "''")
$escapedLogLogical = $LogLogicalName.Replace("'", "''")
$dataFile = [System.IO.Path]::GetFullPath((Join-Path $DataDirectory "$Database.mdf")).Replace("'", "''")
$logFile = [System.IO.Path]::GetFullPath((Join-Path $LogDirectory ("{0}_log.ldf" -f $Database))).Replace("'", "''")

$sql = @"
RESTORE VERIFYONLY FROM DISK = N'$escapedBackupPath' WITH CHECKSUM;
IF DB_ID(N'$escapedDatabaseLiteral') IS NOT NULL
    ALTER DATABASE [$escapedDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$escapedDatabase]
FROM DISK = N'$escapedBackupPath'
WITH MOVE N'$escapedDataLogical' TO N'$dataFile',
     MOVE N'$escapedLogLogical' TO N'$logFile',
     REPLACE, CHECKSUM, RECOVERY, STATS = 10;
ALTER DATABASE [$escapedDatabase] SET MULTI_USER;
DBCC CHECKDB ([$escapedDatabase]) WITH NO_INFOMSGS;
"@

if ($PSCmdlet.ShouldProcess("$Server / $Database", "Restore from $fullBackupPath")) {
    & sqlcmd -S $Server -E -d master -b -V 16 -Q $sql
    if ($LASTEXITCODE -ne 0) {
        throw "Restore or DBCC CHECKDB failed (sqlcmd exit code $LASTEXITCODE). Review database state before retrying."
    }
    Write-Host "Restore and DBCC CHECKDB completed for $Database."
    Write-Warning "Run API health checks and business-workflow smoke tests before switching production traffic."
}
