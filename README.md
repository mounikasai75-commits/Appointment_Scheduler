# Appointment Scheduler

A small ASP.NET Core (.NET 8) minimal API for booking and cancelling provider appointments. It prevents overlapping bookings for the same provider. Data is stored in memory.

## Features

- Book an appointment for a provider with a UTC start and end time
- Reject overlapping active appointments for the same provider (adjacent slots are allowed)
- Cancel an appointment, which frees its time slot
- List all appointments, including cancelled ones
- Swagger UI in the Development environment

## Project structure

```
src/AppointmentScheduler.Api/     API and booking logic (AppointmentService)
tests/AppointmentScheduler.Api.Tests/   xUnit tests
```

## Getting started

See [requirements.md](requirements.md) for prerequisites, then:

```sh
dotnet build AppointmentScheduler.sln
dotnet test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --urls http://127.0.0.1:5090
```

Swagger UI: http://127.0.0.1:5090/swagger

## API

| Method | Endpoint | Description | Responses |
|--------|----------|-------------|-----------|
| GET | `/api/appointments` | List appointments | 200 |
| POST | `/api/appointments` | Book an appointment | 201, 400, 409 |
| DELETE | `/api/appointments/{appointmentId}` | Cancel an appointment | 200, 400, 404, 409 |

Example request:

```sh
curl -i -H 'Content-Type: application/json' \
  -d '{"providerId":"dr-smith","startUtc":"2026-10-24T12:00:00Z","endUtc":"2026-10-24T13:00:00Z"}' \
  http://127.0.0.1:5090/api/appointments
```

Timestamps must be ISO 8601 UTC (ending in `Z` or `+00:00`). Intervals are half-open `[start, end)`. Errors return a code and message.

## Documentation

- [design-spec.md](design-spec.md): design, API behaviour, concurrency
- [assumptions-and-invariant.md](assumptions-and-invariant.md): assumptions and booking invariant
- [setup-and-run.md](setup-and-run.md): detailed setup and verified checks
- [limitations.md](limitations.md): known limitations
- [ai-use-record.md](ai-use-record.md): AI usage record
