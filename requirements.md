# Requirements

## Assessment requirements

Source: the *Appointment Scheduler – AI-Assisted Candidate Assessment* brief. The requirements below are taken from the project's design documents and prompt record, which refer to six acceptance criteria. Check them against the original PDF.

### Goal

Build a small, runnable appointment-booking service that lets a provider's time slots be booked and cancelled without ever double-booking. AI assistance is allowed, and its use must be recorded.

### Functional requirements

| ID | Requirement |
|----|-------------|
| FR-1 | **Book** an appointment with `providerId`, `startUtc` and `endUtc`. The service generates a unique appointment ID (GUID). Success returns `201 Created`. |
| FR-2 | **Cancel** an appointment by ID. Success returns `200 OK`. An unknown ID returns `404`, an already-cancelled one returns `409`, and an invalid ID returns `400`. |
| FR-3 | **List** appointments (`GET /api/appointments`), including cancelled ones. |
| FR-4 | **Reject overlaps.** A booking that overlaps an active appointment for the same provider returns `409 Conflict`. |
| FR-5 | **Free the slot on cancel.** Cancelled appointments no longer block their interval, so the slot can be rebooked. |
| FR-6 | **Validate input.** Reject blank provider IDs, malformed timestamps, non-UTC timestamps, and an end that is not after the start. Return `400` with a structured error (code and message). Invalid input must never change stored state. |

### Booking invariant

- Active appointments for the same provider never overlap.
- Intervals are half-open `[start, end)`. Two intervals conflict only when `existing.Start < requested.End && requested.Start < existing.End`.
- Back-to-back (adjacent) appointments are allowed.
- Different providers are independent.
- Provider IDs are compared ordinally and case-sensitively.

### Time handling

- All timestamps are UTC, in ISO 8601 format ending in `Z` or `+00:00`.
- Local times, non-zero offsets and malformed values are rejected.

### Concurrency

- The conflict check and the insert must happen as one atomic step. Two simultaneous requests for the same slot must produce exactly one booking.
- An in-process lock is enough for this scope.

### Technical constraints

- ASP.NET Core minimal API on .NET 8, using an in-memory store.
- Swagger UI enabled in the Development environment.
- No database, authentication, frontend, notifications, recurring availability, calendar integration or deployment.
- The original brief mentions an Angular client. This submission is API-only, so that is a stated scope deviation.

### Testing and verification

- xUnit tests covering:
  - successful booking
  - overlap rejection
  - adjacency
  - provider isolation
  - cancellation and rebooking
  - missing and already-cancelled IDs
  - invalid input without mutation
  - concurrent booking
- The solution builds with 0 warnings and 0 errors.
- Verified: 17 tests passing.
- Live HTTP checks of booking, overlap, cancel, rebook and invalid timestamps were run against the running API.

### Deliverables

| Deliverable | File |
|-------------|------|
| Working code and tests | `src/`, `tests/`, `AppointmentScheduler.sln` |
| Design spec | [design-spec.md](design-spec.md) |
| Setup and run guide | [setup-and-run.md](setup-and-run.md) |
| Assumptions and invariant | [assumptions-and-invariant.md](assumptions-and-invariant.md) |
| Limitations | [limitations.md](limitations.md) |
| AI-use record (prompts, decisions, verification) | [ai-use-record.md](ai-use-record.md) |

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (verified with 8.0.426)
- macOS, Linux or Windows
- Git

No database or external services are needed. Data is held in memory.

## NuGet packages

Restored automatically by `dotnet restore` (run implicitly by `dotnet build`).

### API (`src/AppointmentScheduler.Api`)

| Package | Version |
|---------|---------|
| Microsoft.AspNetCore.OpenApi | 8.0.32 |
| Swashbuckle.AspNetCore | 6.6.2 |

### Tests (`tests/AppointmentScheduler.Api.Tests`)

| Package | Version |
|---------|---------|
| Microsoft.NET.Test.Sdk | 17.8.0 |
| xunit | 2.5.3 |
| xunit.runner.visualstudio | 2.5.3 |
| coverlet.collector | 6.0.0 |

## Install and verify

```sh
dotnet --version
dotnet restore AppointmentScheduler.sln
dotnet build AppointmentScheduler.sln
dotnet test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj
```
