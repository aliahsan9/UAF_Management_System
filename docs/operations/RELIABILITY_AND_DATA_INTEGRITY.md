# Phase 4 — Reliability and data integrity

## Implemented safeguards

- **Transactions:** admission decisions/conversion, fee challan/payment workflows, attendance finalization, grade appeals/revisions, examination operations, course offering workflows and transcript issuance use explicit database transactions. Critical sequences use serializable isolation or SQL Server update/hold locks where allocation or balance checks must be serialized.
- **Optimistic concurrency:** every `AuditableEntity` maps `RowVersion` to SQL Server `rowversion`. EF Core includes the original token in updates/deletes. A stale write raises `DbUpdateConcurrencyException`; the API returns HTTP 409 with Problem Details. Clients should reload the record and submit a deliberate update.
- **Idempotency:** POST requests may send `Idempotency-Key` (1–128 printable characters). Keys are scoped to the authenticated user (or anonymous client IP), request path/query and request fingerprint. Completed responses up to 1 MiB are replayed for 24 hours. Reusing a key with a different payload returns 409; a concurrent in-flight duplicate returns 409 and `Retry-After`. Responses larger than 1 MiB are marked non-replayable and subsequent duplicate requests are rejected to avoid repeating the operation.
- **Soft delete:** calling EF Core `Remove` for an `AuditableEntity` changes `IsDeleted` and `DeletedAtUtc` instead of physically deleting the row. All normal EF queries exclude deleted auditable records. Restore with the domain `Restore()` method. Use `IgnoreQueryFilters()` only for authorized, audited recovery/administrative workflows.
- **Unique constraints and relationships:** existing entity configurations define unique keys for registration/application numbers, student-account links, course/session/section, course enrollment and other business identifiers; required relationships use restrictive deletes where history must remain. SQL Server constraints—not preflight `AnyAsync` checks—are the final concurrency-safe guard.
- **Background cleanup:** a hosted worker purges expired idempotency reservations in batches every six hours. It does not delete academic, student, financial or audit history.

## Transaction and retry policy

The repository already uses explicit transactions for critical multi-row workflows. Do not enable EF Core `EnableRetryOnFailure` globally until every user-initiated transaction is executed through `Database.CreateExecutionStrategy().ExecuteAsync(...)` (or `ExecuteInTransactionAsync`). SQL Server's retrying execution strategy rejects user-created transactions that are not wrapped by the strategy. This guard is intentional: a blanket retry setting would break current transactional workflows or risk repeating a partially completed operation.

When adding a transaction:
1. Keep all reads that drive the decision and all writes inside the same transaction.
2. Use unique constraints/row locks for contested allocation or balance checks.
3. Keep external network calls outside the transaction; use an outbox for future reliable integration delivery.
4. Only add transient retries after the complete transaction delegate is retry-safe and its commit outcome can be verified.

## Migration rollout

The Phase 4 model adds `IsDeleted`, `DeletedAtUtc` and `RowVersion` to every concrete `AuditableEntity` table and adds `IdempotencyRecords`. Generate an incremental migration against the current database baseline, inspect the generated SQL, and test it against a restored copy before applying it:

```powershell
$env:UAF_DB_CONNECTION_STRING = "<development SQL Server connection string>"
dotnet ef migrations add Phase4ReliabilityAndDataIntegrity --project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj --startup-project backend/UAF/UAF.API/UAF.API.csproj --context UafDbContext --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project backend/UAF/UAF.Infrastructure/UAF.Infrastructure.csproj --startup-project backend/UAF/UAF.API/UAF.API.csproj --context UafDbContext --output artifacts/database/Phase4ReliabilityAndDataIntegrity.sql
```

Do not apply a generated migration to production before reviewing data volume, lock duration, index build behavior, and backup status. The rowversion and soft-delete columns are additive; existing rows receive default values. Verify that any existing migration baseline is committed before generating this incremental migration.

## Backup and recovery runbook

Use SQL Server native full backups with CHECKSUM and compression, store copies outside the database host, encrypt them at rest, and restrict access to the backup identity. Suggested baseline:
- Full backup nightly; retain 30 daily, 12 monthly and one annual copy (adjust to institutional policy).
- Transaction log backups every 15 minutes when using FULL recovery model.
- Run `RESTORE VERIFYONLY` after backup, and perform a full restore drill at least quarterly.
- Record RPO/RTO, backup owner, encryption-key custody, restore approvals and drill results.
- Never treat a successful backup command as proof of recoverability; restore to an isolated SQL Server and run integrity checks.

Example SQL Server commands (execute through a privileged DBA deployment identity, not the API identity):

```sql
BACKUP DATABASE [UafManagementDb]
TO DISK = N'<secure-backup-volume>\\UafManagementDb_FULL.bak'
WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT, STATS = 10;

RESTORE VERIFYONLY
FROM DISK = N'<secure-backup-volume>\\UafManagementDb_FULL.bak'
WITH CHECKSUM;

-- In an isolated recovery environment, restore with the logical data/log names
-- obtained from RESTORE FILELISTONLY, then run:
DBCC CHECKDB ([UafManagementDb]) WITH NO_INFOMSGS;
```

A restore must be rehearsed on an isolated target first. Production restore requires an approved maintenance window, a verified backup chain, correct logical file MOVE paths, and a post-restore application smoke test.

## Data retention

| Data class | Default policy |
|---|---|
| Admission applications and submitted documents | Retain according to the approved admissions/legal schedule; do not auto-delete |
| Student identity, enrollment and academic history | Preserve as institutional record; correct through audited workflows |
| Grades, revisions, transcripts and decision/audit history | Append-only or immutable; never purge via generic cleanup |
| Fee challans, payments and financial audit records | Retain according to statutory/accounting policy; no automatic deletion |
| Soft-deleted operational/reference records | Hidden from ordinary queries; review for archival or authorized purge under a formal retention schedule |
| Idempotency records | 24-hour key lifetime; cleanup worker purges expired records every six hours |
| Application logs | Current Serilog file sink retains 14 rolling files; production should forward logs to managed storage with a separately approved retention policy |

Retention periods for student, applicant, employee and financial data must be approved by the institution's legal/records owner. Do not invent a deletion schedule in code. Any eventual purge must be a separate privileged process with a dry-run report, approval, audit trail and verified backup.
