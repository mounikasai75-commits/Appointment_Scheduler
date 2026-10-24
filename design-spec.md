# Design Specification

## Boundary

`AppointmentService` owns booking validation, conflict checks, cancellation, and in-memory appointment listing. The ASP.NET Core minimal API maps service outcomes to HTTP responses. Swagger UI is enabled in Development. There is no frontend or external database in this project.

## API and data

- `GET /api/appointments` returns `200 OK` with the appointments in memory, including cancelled entries.
- `POST /api/appointments` accepts `providerId`, `startUtc`, and `endUtc`. It returns `201 Created` with the generated appointment, `409 Conflict` for an overlap, or `400 Bad Request` for invalid input.
- `DELETE /api/appointments/{appointmentId}` cancels the appointment. It returns `200 OK`, `404 Not Found` for an unknown ID, `409 Conflict` if already cancelled, or `400 Bad Request` for an invalid ID. Deleting `/api/appointments` without an ID returns a structured `400` response.

An appointment has a generated `Guid` ID, provider ID, UTC start/end, and cancellation flag. Error responses contain a code and message.

## Invariant and concurrency

Appointments for one provider cannot overlap while active. Intervals are half-open `[start, end)`, with a conflict when `existing.StartUtc < requested.EndUtc && requested.StartUtc < existing.EndUtc`. This permits adjacency. Appointments for different providers do not conflict. Cancelled appointments are ignored by conflict checks.

The service validates before mutating the store. A single in-process lock protects conflict-check-plus-insert, cancellation, and listing. It does not coordinate multiple API processes.

## UTC and assumptions

Request timestamps must be ISO 8601 UTC values ending in `Z` or `+00:00`, with seconds and optional fractional seconds. Local, non-zero-offset, and malformed timestamps are rejected. Blank provider IDs and intervals with end less than or equal to start are rejected. Provider IDs are compared ordinally and case-sensitively. Storage resets when the API process exits.