# Phase 6 – Advanced enterprise capabilities

Phase 6 is delivered as reviewable slices, not as placeholder endpoints. This slice establishes:

- Persistent per-user in-app notifications, SMTP email delivery and a configurable HTTP SMS gateway. Delivery failures are stored.
- Authenticated SignalR notification hub with per-user connection groups.
- Generic resource-bound sequential approvals (1–10 stages), role checks, optional assigned-user checks and rejection reasons.
- Append-only workflow transition history.
- Central EF Core audit snapshots for create/update/delete operations, written in the same database transaction as the domain change. Common secret/password/token fields are excluded.
- Paginated SQL Server search for students and admissions.
- Role-protected dashboard KPIs for students, admissions, active sessions and outstanding PKR fee balances.
- Generic private file-storage contract introduced while preserving the admission-document storage interface.

## Configuration

Set `NotificationDelivery__Smtp__Host`, `Port`, `EnableSsl`, `Username`, `Password`, and `From` through deployment secrets. Configure `NotificationDelivery__Sms__Endpoint`, `ApiKey`, and `Sender` for the institution's SMS provider. Never commit provider credentials.

The SignalR hub is `/hubs/notifications`; authenticated clients receive `notification.received` events. Configure a SignalR Redis backplane before running multiple API instances.

## Remaining Phase 6 scope

Admission letters and fee-challan PDFs (transcript PDF already exists); Excel/CSV import/export; broader role-scoped analytics; advanced search/indexing; translated English/Urdu resource strings and RTL frontend presentation; student QR identity verification; and formal document verification workflows still need implementation and tests. The features above are a first integrated slice, not a declaration that all 13 items are complete.

## Release gates

Regenerate and review the EF Core migration for the new tables, add SQL Server integration coverage, configure delivery providers, and verify audit retention/access policies before deployment.
