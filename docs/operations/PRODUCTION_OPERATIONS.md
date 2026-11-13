# Production Operations Runbook

This runbook is an implementation baseline, not a substitute for UAF-approved infrastructure, retention, privacy, or disaster-recovery policies.

## 1. Configuration and secrets

- Keep production connection strings, JWT signing keys, storage credentials, and backup credentials outside source control.
- Use a managed secret store or protected deployment environment variables.
- Use a dedicated SQL Server identity with only the permissions required by the application.
- Do not enable EF Core sensitive-data logging in production.
- Restrict health endpoints at the reverse proxy/network layer where appropriate.

## 2. Structured logs and correlation

The API writes JSON console logs with UTC timestamps and scopes. Each request receives an `X-Correlation-ID` response header. A valid inbound value is retained; invalid or oversized values are replaced. W3C trace IDs are included when available.

Forward stdout/stderr to a centralized log platform. Configure retention, access controls, encryption, alerting, and redaction there. Never log passwords, JWTs, authorization headers, document contents, payment data, or full connection strings. Correlation IDs are diagnostic identifiers, not authentication credentials.

## 3. Health checks

- `GET /health/live`: process liveness only; it does not require SQL Server.
- `GET /health/ready`: readiness; verifies that the configured SQL Server database can be reached.
- Configure orchestration to restart on liveness failure and stop routing traffic on readiness failure.
- Add deployment-specific checks for external email, object storage, identity providers, and other dependencies when those integrations are introduced. Do not report an unused or unconfigured integration as healthy.

Health endpoints intentionally return only component names and status. Restrict public access if infrastructure policy requires it.

## 4. SQL Server backup

The repository includes `scripts/database/Backup-UafDatabase.ps1`.

Example (run from a secured administration host with SQL Server tools installed):

```powershell
./scripts/database/Backup-UafDatabase.ps1 -Server "sql01" -Database "UafManagementDb" -BackupPath "E:\SqlBackups\UafManagementDb_20261113.bak" -WhatIf
./scripts/database/Backup-UafDatabase.ps1 -Server "sql01" -Database "UafManagementDb" -BackupPath "E:\SqlBackups\UafManagementDb_20261113.bak" -Confirm
```

The script uses Windows Integrated Authentication. The backup path is interpreted by the SQL Server service account on the SQL Server host, not by the PowerShell client. Ensure that account can write to the directory. The script uses COPY_ONLY, CHECKSUM, COMPRESSION, then RESTORE VERIFYONLY.

Operational requirements:
- Set backup frequency and retention only after the university's recovery point objective (RPO) and recovery time objective (RTO) are approved.
- Encrypt backups at rest and in transit; copy them to access-controlled off-host storage in a separate failure domain.
- Monitor backup age, size, failures, and storage capacity.
- A successful VERIFYONLY is not a substitute for a restore test.

## 5. Restore and disaster recovery

Never test restore procedures against the production database. Restore into an isolated recovery environment with network access restricted.

1. Select a known-good backup and verify its checksum.
2. Inspect logical file names using `RESTORE FILELISTONLY FROM DISK = N'<backup-path>'`.
3. Restore to a separate staging database using `RESTORE DATABASE [UafRecoveryTest] FROM DISK = N'<backup-path>' WITH MOVE N'<logical-data-name>' TO N'<staging-data-file>', MOVE N'<logical-log-name>' TO N'<staging-log-file>', CHECKSUM, RECOVERY;`.
4. Run integrity checks such as `DBCC CHECKDB([UafRecoveryTest]) WITH NO_INFOMSGS;`.
5. Run application smoke tests and verify critical counts/workflows without exposing student data.
6. Record restore duration, backup timestamp, test outcome, and any data-loss window.
7. Only an authorized DBA and change approver may execute a production recovery.

Do not use `WITH REPLACE`, `SINGLE_USER`, or overwrite a production database as part of an unattended deployment.

## 6. Migrations

- Generate migrations using the repository's scripts under `scripts/database`.
- Review generated C# and SQL before merge, especially destructive operations, data transformations, indexes, and foreign keys.
- Take and verify a backup before applying production migrations.
- Apply migrations through a controlled release/DBA identity, not automatically at API startup.
- Run the idempotent script against a restored staging copy first.
- Keep migration history and deployment logs.

## 7. Query performance and indexes

- Review `scripts/database/Enable-UafQueryStore.sql` with the DBA and adjust storage/retention settings to the approved operations policy before execution.
- Run `scripts/database/Inspect-UafPerformance.sql` periodically using a read-only diagnostic identity to review Query Store duration, logical reads, index usage, and missing-index hypotheses.
- Enable SQL Server Query Store in the target database and retain its data according to the approved operations policy.
- Capture actual execution plans for slow or high-CPU queries.
- Review logical reads, duration, CPU, spills, blocking, and parameter sensitivity before adding indexes.
- Prefer indexes that support measured filters, joins, and sort orders; avoid speculative or duplicate indexes.
- Recheck write overhead and index fragmentation after deployment.
- Never log SQL parameter values or enable EF Core sensitive-data logging in production.
- Establish baseline p50/p95/p99 API latency, SQL CPU, connection-pool pressure, deadlocks, and error rate before setting alerts.

## 8. Audit retention and privacy

Academic, admission, fee, grade, attendance, and transcript audit records are business records. Do not purge them using a generic age-based job.

Before implementing automated retention, obtain approved schedules from the university's records, legal, and privacy authorities. Define per-record-class retention, legal holds, archival format, access controls, deletion approval, and proof of disposition. Until then, retain audit rows and monitor table growth. Audit tables should be append-only at the application boundary and protected by least-privilege database permissions.

## 9. Monitoring and incident response

At minimum alert on:
- Readiness failures and repeated liveness restarts.
- HTTP 5xx rate, request latency, and sustained 429 responses.
- SQL Server connectivity, CPU, storage, deadlocks, failed jobs, and backup age.
- Unexpected changes in database size and audit-table growth.
- Repeated authentication failures and authorization denials.

Every incident record should include the correlation ID, UTC timestamps, deployment version, affected component, impact, mitigation, and follow-up actions. Do not include secrets or student personal data in incident tickets.
