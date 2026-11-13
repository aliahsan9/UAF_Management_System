# Performance and caching

- Academic session and course catalogue lists use .NET HybridCache with five-minute absolute expiration plus one-minute sliding expiration in the local memory front-cache.
- Cache entries carry the `academic-reference-data` tag. Successful writes to academic-catalog or academic-structure endpoints evict the tag.
- Configure `ConnectionStrings__Redis` and optionally `Redis__InstanceName` to add Redis as the HybridCache distributed backing store. Without Redis, in-process memory caching remains available.
- Do not cache user-specific, permission-sensitive, financial, attendance, grading, or admission workflow data without user-aware keys and a corresponding invalidation design.
- Brotli and Gzip response compression are enabled, including HTTPS.
- Student list queries support bounded server-side pagination, optional search across registration number/name/email, and allow-listed sorting by registration number, full name, or enrollment date. A stable ID tie-breaker makes ordering deterministic.
- Student, employee, course, and course-offering list queries already use server-side filters and/or pagination. Existing read-only EF queries use `AsNoTracking()`; tracked entity reads remain tracked where workflows mutate aggregates.
- Course-offering list responses obtain active enrollment counts through one grouped query instead of one count query per offering (N+1).
- Composite indexes cover common student academic-program/status/registration and course/session/status offering filters. Validate execution plans against production-like data before adding more indexes; indexes increase write cost.
