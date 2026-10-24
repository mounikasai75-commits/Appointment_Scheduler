# Appointment Scheduler Requirements

## Purpose and scope

Implement provider appointment booking and cancellation with conflict prevention. This is a bounded service slice, not a complete scheduling product.

The workspace had no starter project. The implementation is .NET 8, C#, ASP.NET Core Minimal API, Swagger, an in-memory store and xUnit. The design is kept proportionate to this single-service workflow.

The requirements below are derived from the project's design documents and prompt record. Check them against the original assessment PDF.

### In scope

- Book an appointment for a provider with a UTC start and end time.
- Reject a booking that overlaps an active appointment of the same provider.
- Cancel an appointment, which frees its time slot, and allow rebooking.
- List appointments.
- Return clear, structured results for invalid requests and repeated cancellations without corrupting state.
- Focused automated tests for the acceptance criteria.
- Document the service boundary, API and data design, invariant, concurrency approach, assumptions, tradeoffs, setup and test commands, AI use, and limitations.

### Out of scope

Frontend (the brief mentions an Angular client; this submission is API-only), authentication, persistent database, notifications, recurring availability, calendar integration, deployment, multi-process coordination, and a configurable policy engine.

## Explicit requirements

1. A provider's schedule is the unit of conflict: appointments conflict only within the same provider.
2. Active appointments for the same provider must never overlap.
3. Booking an interval that overlaps an active appointment of the same provider is rejected with `409 Conflict`.
4. Adjacent appointments (one ends exactly when the next starts) are allowed.
5. Appointments for different providers never conflict.
6. Cancelling an appointment frees its interval, and cancelled appointments are ignored by conflict checks.
7. All timestamps are UTC. Local, non-zero-offset and malformed timestamps are rejected.
8. Blank provider IDs and intervals where end is not later than start are rejected.
9. Invalid requests and invalid or repeated cancellations must not corrupt stored state.
10. Conflict check plus insert must be atomic: concurrent requests for the same slot produce exactly one booking.
11. Tests cover successful booking, overlap, adjacency, provider isolation, cancellation and rebooking, missing and already-cancelled IDs, invalid input without mutation, and concurrent booking.
12. At least one engineering activity uses AI. Record prompts, response summaries, accept/edit/reject decisions, files or decisions informed, and verification evidence.
13. Deliver runnable scoped code and tests, a proportionate design specification, an AI-use record, setup and test instructions, and a concise summary of limitations.

## Finalized business rules

### Appointment states

- **Active:** the appointment holds its interval for its provider.
- **Cancelled:** the appointment no longer blocks any interval. It stays in the list with its cancelled flag set.

An appointment has a generated `Guid` ID, provider ID, UTC start, UTC end, and a cancelled flag.

### Overlap rule

Intervals are half-open `[start, end)`. Two appointments of the same provider conflict when:

`existing.StartUtc < requested.EndUtc && requested.StartUtc < existing.EndUtc`

Provider IDs are compared ordinally and case-sensitively.

### Booking

1. Validate first. Invalid requests are rejected without touching the store.
2. Under a lock, check the requested interval against the provider's active appointments.
3. If there is no conflict, create the appointment with a generated ID and return it.
4. If there is a conflict, return a conflict result and change nothing.

### Cancellation

1. A valid ID of an active appointment is marked cancelled and returns success.
2. A valid but unknown ID returns not found.
3. An appointment that is already cancelled returns a conflict result and changes nothing.
4. A missing or unparseable ID returns an invalid-ID error.

### Time handling

Accepted format is ISO 8601 UTC ending in `Z` or `+00:00`, with seconds and optional fractional seconds.

### API and result mapping

| Method | Endpoint | Outcome | Status |
|--------|----------|---------|--------|
| GET | `/api/appointments` | List, including cancelled | 200 |
| POST | `/api/appointments` | Booked | 201 |
| POST | `/api/appointments` | Overlap (`conflict`) | 409 |
| POST | `/api/appointments` | Invalid or null body (`invalid_input`) | 400 |
| DELETE | `/api/appointments/{appointmentId}` | Cancelled | 200 |
| DELETE | `/api/appointments/{appointmentId}` | Unknown ID | 404 |
| DELETE | `/api/appointments/{appointmentId}` | Already cancelled | 409 |
| DELETE | `/api/appointments/{appointmentId}` | Invalid ID (`invalid_id`) | 400 |
| DELETE | `/api/appointments` | No ID given (`invalid_id`) | 400 |

Error responses contain a code and a message.

## Acceptance criteria mapped to behavior and verification

| AC | Required behavior | Minimum verification |
|----|-------------------|----------------------|
| 1 | A valid booking for a free interval succeeds and returns a generated ID. | Test booking and the stored appointment. |
| 2 | An overlapping booking for the same provider is rejected. Adjacent and other-provider bookings are accepted. | Test overlap, adjacency and provider isolation. |
| 3 | Cancelling frees the slot, and the interval can be rebooked. | Test cancel then rebook. |
| 4 | Invalid input, unknown IDs and repeated cancellations return clear results without changing state. | Test invalid timestamps, blank provider, bad IDs, unknown and already-cancelled IDs. Verify the stored state is unchanged. |
| 5 | Concurrent requests cannot double-book a slot. | Concurrent booking test yields exactly one success. |
| 6 | The solution builds cleanly and tests pass. | Run `dotnet build` and `dotnet test`. Verified: 0 warnings, 0 errors, 17 passed. |
| 7 | AI was used, independently reviewed and verified, and recorded. | See [ai-use-record.md](ai-use-record.md). |

## Invariants to preserve

- Active appointments of one provider never overlap.
- Intervals are half-open, so adjacency is allowed.
- Cancelled appointments never block a booking.
- Appointments of different providers are independent.
- Failed, invalid or duplicate operations leave no partial state change.
- Conflict check and insert happen atomically.

## State transitions

| Current state | Request | Condition | Result |
|---------------|---------|-----------|--------|
| None | Book | Invalid input | Reject (400). No change. |
| None | Book | Overlaps an active appointment of the same provider | Reject (409). No change. |
| None | Book | Free interval, adjacent, or other provider | Active (201). |
| Active | Cancel | Valid ID | Cancelled (200). Interval is freed. |
| Cancelled | Cancel | Repeated request | Remain Cancelled (409). No change. |
| None | Cancel | Unknown ID | Reject (404). No change. |
| None | Cancel | Invalid or missing ID | Reject (400). No change. |
| Cancelled interval | Book | Same provider and time | Active (201). Rebooking is allowed. |

## Assumptions and decisions

| Topic | Decision |
|-------|----------|
| Persistence | In-memory store, lost when the process exits. |
| Concurrency | One in-process lock covers booking, cancellation and listing. It does not coordinate several API processes. |
| Provider IDs | Ordinal, case-sensitive comparison. |
| Timestamps | Strict UTC only, rejecting local and non-zero-offset values. |
| Cancellation | Soft cancel. The appointment stays in the list. |
| Missing cancellation ID | Structured 400 instead of a bare 405. |
| Frontend | Not included. API-only scope deviation. |

## Verification and deliverables checklist

- [x] Focused automated tests cover every scenario in AC 1 to 5 (17 passing).
- [x] The solution builds with 0 warnings and 0 errors.
- [x] The booking invariant is applied consistently, including rebooking after cancellation.
- [x] Live HTTP checks of booking, overlap, cancel, rebook and invalid input were run.
- [x] Setup and test commands are documented in [setup-and-run.md](setup-and-run.md).
- [x] AI prompts, decisions and verification are recorded in [ai-use-record.md](ai-use-record.md).
- [x] Limitations are summarized in [limitations.md](limitations.md).

## Deliverables

| Deliverable | File |
|-------------|------|
| Working code and tests | `src/`, `tests/`, `AppointmentScheduler.sln` |
| Design spec | [design-spec.md](design-spec.md) |
| Setup and run guide | [setup-and-run.md](setup-and-run.md) |
| Assumptions and invariant | [assumptions-and-invariant.md](assumptions-and-invariant.md) |
| Limitations | [limitations.md](limitations.md) |
| AI-use record | [ai-use-record.md](ai-use-record.md) |

## Environment prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (verified with 8.0.426)
- macOS, Linux or Windows, and Git
- No database or external services

### NuGet packages

Restored automatically by `dotnet restore`.

| Project | Package | Version |
|---------|---------|---------|
| API | Microsoft.AspNetCore.OpenApi | 8.0.32 |
| API | Swashbuckle.AspNetCore | 6.6.2 |
| Tests | Microsoft.NET.Test.Sdk | 17.8.0 |
| Tests | xunit | 2.5.3 |
| Tests | xunit.runner.visualstudio | 2.5.3 |
| Tests | coverlet.collector | 6.0.0 |

### Install and verify

```sh
dotnet --version
dotnet restore AppointmentScheduler.sln
dotnet build AppointmentScheduler.sln
dotnet test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj
```
