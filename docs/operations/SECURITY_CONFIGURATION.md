# Security and authentication operations

## Identity and JWT

- ASP.NET Core Identity owns password hashing, user lifecycle, role membership and lockout.
- Public registration assigns only the `Student` role. Never accept role names or permission lists from a public registration payload.
- Password policy: at least 12 characters with upper/lowercase, digit and non-alphanumeric characters.
- Failed password checks use Identity lockout: five consecutive failures followed by a 15-minute lockout.
- Access tokens are short-lived (configure `Jwt:AccessTokenMinutes`, default 15 minutes). The signing key must be at least 32 UTF-8 bytes.
- Configure `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` and `ConnectionStrings__UafDatabase` as deployment secrets/environment variables.

## Refresh-token lifecycle

- Refresh tokens are cryptographically random, returned only as a Secure, HttpOnly, SameSite=Strict `__Host-uaf-refresh` cookie, and stored in SQL only as SHA-256 hashes.
- Refresh rotates the token on every successful use. The old token is revoked; reuse of a revoked token revokes all active refresh sessions for that account.
- Browser refresh/revoke requests must send `X-Refresh-Request: 1`. The custom header is a CSRF defense in addition to the strict cookie policy and exact CORS allow-list.
- Access tokens should be held in memory by browser clients, not localStorage/sessionStorage. Do not log or persist raw tokens.
- The refresh-token table requires an EF migration before deployment.

## RBAC and permissions

- Identity roles are seeded by the existing controlled role-seeding workflow. Local Development enables this through `Identity:SeedRolesOnStartup`; in Staging/Production, run the seed as a controlled provisioning step and keep automatic startup seeding disabled.
- Existing role-based endpoint policies remain in place.
- Fine-grained policies are available as `Permission:<permission-name>` (for example `Permission:finance.manage`).
- Permission grants are centralized in `PermissionAuthorizationHandler`; review that mapping whenever a role or permission changes. Resource ownership (for example, a student accessing only their own records) must still be enforced in the application service/repository, not by a broad role permission alone.

## Rate limiting and CORS

- Login, registration, refresh and revoke share a per-client-IP fixed-window limit of 10 requests/minute.
- Identity lockout independently slows repeated invalid password attempts per account.
- Configure exact browser origins using `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. Do not use wildcard origins with credentials.
- The API allows only the headers required by the frontend, including Authorization and the refresh CSRF header.

## Upload security

- Admission uploads are limited to 10 MB (11 MB multipart request ceiling), accepted extensions are PDF/JPEG/PNG, declared MIME type must match extension, and file signatures are checked before storage.
- Files are stored outside wwwroot under opaque server-generated keys; downloads are authorization-checked and sent with `nosniff`.
- Production deployments should add malware scanning/quarantine and private object storage before accepting untrusted public traffic at scale. Do not serve uploaded files inline from the API origin.

## Audit events and security headers

- `SecurityAuditMiddleware` emits structured audit events for mutating HTTP requests, including actor ID, roles, method, route, response status, source IP and correlation ID. It intentionally excludes request bodies, query strings and credentials.
- Domain workflows should continue writing their dedicated append-only business audit records (admission decisions, attendance changes, grade revisions, etc.). HTTP telemetry is not a replacement for those records.
- The API adds nosniff, frame denial, strict referrer, permissions, CSP, COOP and CORP headers; HSTS is enabled outside Development.

## Secrets management and deployment

1. Local development: use `dotnet user-secrets` from the API project directory. Example:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Issuer" "UAF.ManagementSystem"
   dotnet user-secrets set "Jwt:Audience" "UAF.ManagementSystem.Client"
   dotnet user-secrets set "Jwt:SigningKey" "<generate-a-random-secret-of-at-least-32-bytes>"
   dotnet user-secrets set "ConnectionStrings:UafDatabase" "<local-connection-string>"
   ```
2. CI/CD: store production values in GitHub Actions Secrets or the deployment platform's secret manager; inject them at runtime. Never print secrets in workflow logs.
3. Production: prefer a managed secret store (Azure Key Vault or equivalent), use managed identity where supported, rotate signing keys and database credentials, and document a controlled key-rotation procedure.
4. If a secret is ever committed, removing it from the latest commit is insufficient: revoke/rotate it immediately and then clean repository history according to incident response policy.
5. Keep only non-sensitive defaults in appsettings files. Never commit real JWT keys, passwords, API keys, private certificates, or production connection strings.

## Required rollout steps

- Generate and review the first EF Core migration from the complete current model (this repository does not yet contain a committed migration baseline). It must include `RefreshTokens` and all prior model changes. Do not apply an ad-hoc partial migration.
- Configure production JWT secrets, database connection, allowed CORS origins and private document storage path.
- Verify HTTPS termination, secure cookies, rate limiting, role seed, and authorization policies in a staging environment.
- Configure trusted reverse-proxy forwarded headers before relying on client IP-based rate limiting behind a load balancer.
- Enable malware scanning and durable private storage before production admission uploads.
