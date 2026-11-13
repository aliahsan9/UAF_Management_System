[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "Medium")]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [string] $BackupPath,

    [string] $Server = "localhost"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd was not found. Install the Microsoft SQL Server command-line tools."
}

$fullBackupPath = [System.IO.Path]::GetFullPath($BackupPath)
$escapedPath = $fullBackupPath.Replace("'", "''")
$escapedDatabase = $Database.Replace("]", "]]")
$sql = @"
BACKUP DATABASE [$escapedDatabase]
TO DISK = N'$escapedPath'
WITH COPY_ONLY, INIT, CHECKSUM, COMPRESSION, STATS = 10;
RESTORE VERIFYONLY
FROM DISK = N'$escapedPath'
WITH CHECKSUM;
"@

if ($PSCmdlet.ShouldProcess("$Server / $Database", "Create and verify SQL Server backup at $fullBackupPath")) {
    & sqlcmd -S $Server -E -d master -b -V 16 -Q $sql
    if ($LASTEXITCODE -ne 0) {
        throw "SQL Server backup or RESTORE VERIFYONLY failed (sqlcmd exit code $LASTEXITCODE)."
    }
    Write-Host "Backup created and verified: $fullBackupPath"
    Write-Warning "The backup file is written by the SQL Server service on the SQL Server host. Copy it to encrypted off-host storage and test restores regularly."
}
