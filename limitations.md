# Limitations and Follow-up Risks

- Appointment data is held only in memory and is lost when the API process exits.
- The lock is process-local; multiple API instances would not share booking state or coordinate conflicts.
- The project is an API-only service slice. It does not include authentication, a persistent database, notifications, recurring availability, calendar integration, or deployment.
- The xUnit suite has 17 passing tests. Swagger and booking lifecycle/error behavior were also checked with live HTTP requests. Full deployment, multi-process concurrency, and persistence behavior are not applicable or verified for this in-memory prototype.