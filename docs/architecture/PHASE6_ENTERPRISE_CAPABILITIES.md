# Phase 6 – Advanced enterprise capabilities

Phase 6 is implemented as independently reviewable capabilities in the modular monolith. The current branch contains a first end-to-end foundation for all 13 requested items; each section below distinguishes implemented scope from production hardening still required.

## Capability status

| # | Capability | Implemented scope | Remaining hardening |
|---:|---|---|---|
| 47 | Notifications | Persistent per-user in-app inbox; SMTP email sender; configurable HTTP SMS gateway; delivery success/failure persisted | Add transactional outbox, retry/backoff, delivery templates, provider callbacks and event-driven integration with admissions/fees |
| 48 | Real-time updates | Authenticated SignalR hub at `/hubs/notifications`; user-scoped connection groups; `notification.received` event | Add Redis backplane for multi-instance deployment and frontend reconnect/unread synchronization |
| 49 | File storage abstraction | Generic private `IFileStorage` contract; existing local provider implements it; admission storage interface remains backward compatible | Add cloud object-storage provider, malware scanning, retention and lifecycle policies |
| 50 | PDF generation | Admission acceptance letter, fee challan and immutable transcript PDFs | Add institutional branding/signatures, localized templates and full SQL integration coverage |
| 51 | Excel/CSV import/export | Student XLSX/CSV export and import with header validation, row validation, duplicate registration checks and all-or-nothing persistence | Extend to other entities, background jobs for large files, import preview and downloadable error reports |
| 52 | Dashboard analytics | Protected dashboard counts for students, admissions, active sessions and outstanding challan balances | Add date filters, department/campus scoping, trend series, caching and richer finance/admissions KPIs |
| 53 | Approval workflows | Generic sequential 1–10 stage workflow with required role and optional assigned approver; rejection requires reason | Bind approval definitions to specific fee, HR, admission and academic business actions; configure delegation/escalation |
| 54 | Workflow history | Append-only state-transition history with actor, state, time and comments | Add history integration to every existing legacy lifecycle, including admission, grades and payments |
| 55 | Central audit trail | EF Core interceptor captures domain create/update/soft-delete snapshots in the same transaction; common password/token/secret fields excluded; append-only application guard | Add tamper-evident retention/archive, actor attribution for system jobs, database-level protection and audit export |
| 56 | Search | Paginated SQL Server search for students and admissions | Add normalized search, filters, indexes/full-text search, relevance and query telemetry |
| 57 | English/Urdu | Accept-Language request localization, English and Urdu resource files for shared import/verification messages | Frontend translations, Urdu RTL layout, localized PDF templates and broader resource coverage |
| 58 | QR generation | HMAC-signed student verification URL rendered as PNG QR; public endpoint returns limited identity details | Add QR lifecycle/revocation and institution-specific verification page/branding |
| 59 | Document verification | Signed public verification links for admission documents; verifies stored document hash and reviewer status without exposing the private file | Extend to certificates/transcripts, add tamper-evident document hashes and verification history |

## API routes added

- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{id}/read`
- `POST /api/v1/workflows`
- `POST /api/v1/workflows/{id}/approve`
- `POST /api/v1/workflows/{id}/reject`
- `GET /api/v1/workflows/{id}/history`
- `GET /api/v1/audit-trail`
- `GET /api/v1/search/students`
- `GET /api/v1/search/admissions`
- `GET /api/v1/analytics/dashboard`
- `GET /api/v1/admissions/{applicationId}/admission-letter.pdf`
- `GET /api/v1/fees/challans/{challanId}/pdf`
- `GET /api/v1/data-exchange/students?format=csv|xlsx`
- `POST /api/v1/data-exchange/students/import?academicProgramId={id}`
- `GET /api/v1/students/{studentId}/verification-qr`
- `GET /api/v1/verification/students/{registrationNumber}?signature=...`
- `GET /api/v1/admission-documents/{documentId}/verification-link`
- `GET /api/v1/verification/admission-documents/{documentId}?signature=...`
- SignalR: `/hubs/notifications`

## Configuration

Set the following via environment variables or a secret store:

- `NotificationDelivery__Smtp__Host`, `Port`, `EnableSsl`, `Username`, `Password`, `From`
- `NotificationDelivery__Sms__Endpoint`, `ApiKey`, `Sender`
- `PublicBaseUrl` for QR and document verification links behind reverse proxies

Never commit provider credentials. The student QR and document verification signatures are derived from the validated JWT signing key with a separate purpose string; configure a separate verification secret before a production security review.

## Authorization and privacy

- Student directory search, student exports and QR generation are staff-only (student-management permission).
- Admission search and document verification-link generation require admissions staff permissions.
- Audit trail reads are SuperAdmin-only.
- Public verification endpoints return only limited identity/document metadata and require valid HMAC signatures; invalid signatures return not found.
- Import and export operations are capped and paginated/streamed at the API boundary; large-scale asynchronous import remains future work.

## Release gates

- Regenerate and review the EF Core migration for notification, workflow and audit tables before deployment.
- Run SQL Server integration tests for notification persistence, approval concurrency, audit interception and data import.
- Configure SMTP/SMS secrets and verify provider delivery in a non-production environment.
- Configure SignalR scale-out before multi-instance hosting.
- Review data-retention, privacy and legal requirements for audit snapshots and student identity verification.
