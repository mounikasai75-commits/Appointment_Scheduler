# AI Use Record

## Prompts used

The user supplied the assessment PDF and issued the eight prompts in `project-prompts.txt` in this order. The role/context/task/guardrail/expected-output labels below organize each prompt; they are not claimed to be extra verbatim words from the user.

### 1. Inspect requirements and environment

- **Role:** Project analyst.
- **Context:** Appointment-booking requirements are in `/Users/sudhikshasingh/Downloads/Appointment-Scheduler-AI-Assisted-Candidate-Assessment.pdf`; the target is `Appointment_Scheduler`.
- **Task:** Read the requirements, check the installed .NET SDK, and suggest a small runnable ASP.NET Core API structure.
- **Guardrails:** Make no file changes yet; avoid guessing versions or adding unrelated features.
- **Expected output:** A short summary of requirements, available SDK, and proposed API structure.

### 2. Design the API

- **Role:** API designer.
- **Context:** The service books and cancels provider appointments using an in-memory store.
- **Task:** Define service boundary, request/result shapes, routes, status codes, UTC rules, validation, overlap behavior, and concurrency.
- **Guardrails:** Use half-open intervals `[start, end)`; do not edit files yet; keep the design focused.
- **Expected output:** Concise design, assumptions, and files to create or change.

### 3. Scaffold the solution

- **Role:** .NET project scaffolder.
- **Context:** The folder needs a runnable API and tests; an installed supported .NET SDK is available.
- **Task:** Create a .NET solution with an ASP.NET Core API, Swagger UI in Development, and an xUnit project referencing the API.
- **Guardrails:** No database or unrelated features.
- **Expected output:** Solution/project files and a stated SDK version.

### 4. Implement booking and cancellation

- **Role:** ASP.NET Core API implementer.
- **Context:** Use the agreed API design and in-memory store.
- **Task:** Validate provider IDs and strict UTC timestamps; reject non-positive durations; implement conflict-safe booking, cancellation, generated appointment IDs, and clear results.
- **Guardrails:** Invalid input must not mutate state. Use `[start, end)` overlap logic; ignore cancelled appointments; protect check-and-insert and cancellation with an in-process lock.
- **Expected output:** Runnable API endpoints and service code implementing the invariant.

### 5. Add focused tests

- **Role:** Test engineer.
- **Context:** The API service and xUnit project are in place.
- **Task:** Test successful booking, overlap, adjacency, provider isolation, cancellation/rebooking, missing/already-cancelled IDs, invalid input without mutation, and concurrent requests.
- **Guardrails:** Keep tests focused; report only commands actually run and their real results.
- **Expected output:** Focused xUnit coverage and test command/results.

### 6. Build and exercise the API

- **Role:** Verification engineer.
- **Context:** The API should run in Development with Swagger.
- **Task:** Build the solution, inspect Swagger routes, and exercise booking, overlap, cancellation, rebooking, and invalid timestamps.
- **Guardrails:** Do not claim unrun checks passed; use actual project paths and report actual HTTP statuses.
- **Expected output:** Exact commands, observed statuses, and results.

### 7. Review and fix confirmed findings

- **Role:** Code reviewer and fix implementer.
- **Context:** Review the implementation and tests against the booking invariant.
- **Task:** Check interval boundaries, provider filtering, cancelled bookings, races, state mutation, UTC parsing, and ID/error handling; fix confirmed issues and rerun tests/build.
- **Guardrails:** Report concrete findings; make only in-scope fixes; do not broaden the feature.
- **Expected output:** Findings or an explicit no-findings result, fixes if needed, and fresh verification output.

### 8. Document and audit

- **Role:** Technical writer and acceptance auditor.
- **Context:** Use the actual project files and observed verification results; compare with the supplied PDF.
- **Task:** Create the design spec, setup/run guide, assumptions/invariant note, limitations, and AI-use record; check each of the six acceptance criteria and all deliverables.
- **Guardrails:** Do not invent prompts or results. Mark unverified items accurately. Explicitly identify any scope deviation; this API-only project has no Angular client.
- **Expected output:** Five concise documents and a criterion-by-criterion status report with evidence.

The full prompt wording remains in `project-prompts.txt`. The eight prompts were used as the project workflow; the role/context/task labels above are an organizational summary. The separate request to create the five documents is covered by prompt 8 and is not represented as a ninth prompt.

## AI response summary and decisions

- Accepted: the no-starter fallback uses a small ASP.NET Core API, Swagger, an in-memory appointment service, and an xUnit test project; no frontend or database was added.
- Accepted: the service uses explicit UTC timestamps, half-open intervals, generated IDs, and an in-process lock around booking and cancellation.
- Adapted after review: the API initially returned an unstructured `405` when the cancellation ID was omitted and a Development exception response for a `null` POST body. Both were changed to structured `400` error responses.
- Rejected/not added: frontend, authentication, persistent database, notifications, recurring availability, calendar integration, deployment, or unrelated scheduling features.
- Files informed: `src/AppointmentScheduler.Api/Program.cs`, `src/AppointmentScheduler.Api/Appointments/AppointmentService.cs`, `src/AppointmentScheduler.Api/Appointments/AppointmentModels.cs`, `tests/AppointmentScheduler.Api.Tests/AppointmentServiceTests.cs`, and these five project documents.

## Verification evidence

- SDK: `"$HOME/.dotnet/dotnet" --list-sdks` reported `8.0.426`.
- `"$HOME/.dotnet/dotnet" test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj` from the project directory: 17 passed, 0 failed.
- `"$HOME/.dotnet/dotnet" build AppointmentScheduler.sln` from the project directory: succeeded, 0 warnings, 0 errors.
- Development API command: `ASPNETCORE_ENVIRONMENT=Development "$HOME/.dotnet/dotnet" run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --urls http://127.0.0.1:5090`.
- Swagger UI returned HTTP 200; the OpenAPI document listed the appointment collection and ID routes.
- Live API requests observed: booking 201, same-provider overlap 409, cancellation 200, rebooking after cancellation 201, and missing UTC marker 400.
- Follow-up live checks after review fixes: omitted cancellation ID returned structured 400 `invalid_id`; `null` POST body returned structured 400 `invalid_input`.
- No frontend or browser checks apply to this API-only project.