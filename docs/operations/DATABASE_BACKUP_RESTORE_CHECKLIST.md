# SQL Server backup and restore checklist

## Before enabling production backups
- [ ] Confirm database recovery model and required RPO/RTO with the database owner.
- [ ] Configure a dedicated least-privilege backup identity.
- [ ] Store backup files on a protected volume and replicate off-host/off-site.
- [ ] Encrypt backup storage and protect encryption certificates/keys separately.
- [ ] Configure full and transaction-log backup schedules appropriate to the recovery model.
- [ ] Alert on backup failures, missed log backups, low disk space and failed verification.

## Each backup run
- [ ] Run SQL Server `BACKUP DATABASE ... WITH CHECKSUM, COMPRESSION`.
- [ ] Run `RESTORE VERIFYONLY ... WITH CHECKSUM`.
- [ ] Capture database name, backup start/end, backup type, file size, checksum result and operator/job identity.
- [ ] Confirm off-host replication completed.
- [ ] Apply retention only after a newer verified copy exists.

## Restore drill
- [ ] Select a recent backup chain and record the target recovery point.
- [ ] Restore to an isolated SQL Server instance, never over production for a drill.
- [ ] Use `RESTORE FILELISTONLY` and explicit `MOVE` paths for data and log files.
- [ ] Run `DBCC CHECKDB` after restore.
- [ ] Apply/reconcile the intended migration level.
- [ ] Run API health checks and smoke tests for admissions, student lookup, fee balances, examination results and transcript verification.
- [ ] Record actual RPO/RTO, issues, evidence and corrective actions.

## Production recovery
Production recovery requires incident commander and database-owner approval. Preserve the damaged database and logs for investigation where possible. Select point-in-time recovery based on the verified full/differential/log chain, communicate the recovery point, restore to a controlled target, validate integrity and application workflows, then switch traffic. Never delete the source backup chain or overwrite the only known-good copy during an incident.
