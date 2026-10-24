# Setup and Run

## Prerequisite

The installed SDK is .NET 8.0.426 at `$HOME/.dotnet`. From a new shell, use the full executable path below or add `$HOME/.dotnet` to `PATH`.

## Build and tests

Run from the `Appointment_Scheduler` directory:

```sh
"$HOME/.dotnet/dotnet" build AppointmentScheduler.sln
"$HOME/.dotnet/dotnet" test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj
```

Verified results: build succeeded with 0 warnings and 0 errors; tests passed with 17 passed, 0 failed.

## Run the API

From the project directory:

```sh
ASPNETCORE_ENVIRONMENT=Development "$HOME/.dotnet/dotnet" run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --urls http://127.0.0.1:5090
```

Swagger UI is available at `http://127.0.0.1:5090/swagger`.

## Verified Swagger and HTTP checks

Swagger UI and OpenAPI commands:

```sh
curl -sS -o /dev/null -w 'Swagger UI HTTP %{http_code}\n' http://127.0.0.1:5090/swagger/index.html
curl -fsS http://127.0.0.1:5090/swagger/v1/swagger.json | grep -o '"/api/appointments[^" ]*"' | sort -u
```

Observed: Swagger UI returned `200`; OpenAPI listed `/api/appointments` and `/api/appointments/{appointmentId}`.

The following requests were run against the live API:

```sh
curl -sS -i -H 'Content-Type: application/json' -d '{"providerId":"docs-provider","startUtc":"2026-10-24T12:00:00Z","endUtc":"2026-10-24T13:00:00Z"}' http://127.0.0.1:5090/api/appointments
curl -sS -i -H 'Content-Type: application/json' -d '{"providerId":"docs-provider","startUtc":"2026-10-24T12:30:00Z","endUtc":"2026-10-24T13:30:00Z"}' http://127.0.0.1:5090/api/appointments
curl -sS -i -X DELETE http://127.0.0.1:5090/api/appointments/d20dbb6a-26e1-49a5-a936-ba8c230d98df
curl -sS -i -H 'Content-Type: application/json' -d '{"providerId":"docs-provider","startUtc":"2026-10-24T12:00:00Z","endUtc":"2026-10-24T13:00:00Z"}' http://127.0.0.1:5090/api/appointments
curl -sS -i -H 'Content-Type: application/json' -d '{"providerId":"docs-provider","startUtc":"2026-10-24T12:00:00","endUtc":"2026-10-24T13:00:00Z"}' http://127.0.0.1:5090/api/appointments
```

Observed in order: `201 Created`; `409 Conflict` with code `conflict`; `200 OK` with `Appointment cancelled.`; `201 Created` for rebooking; `400 Bad Request` with code `invalid_input`.

Additional error handling checks returned `400 Bad Request` with code `invalid_id` for `DELETE /api/appointments` without an ID, and `400 Bad Request` with code `invalid_input` for a `null` POST body. These checks were run after the corresponding fixes.