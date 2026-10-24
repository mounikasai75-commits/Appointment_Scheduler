# Test coverage map

All tests are in `tests/AppointmentScheduler.Api.Tests/AppointmentServiceTests.cs` (xUnit) and exercise `AppointmentService` directly. Each test creates its own `AppointmentService` with a fresh in-memory store, so no test depends on execution order or shared state. Nothing is mocked. Tests assert the stored appointments after each operation, not only the returned result.

Run: `dotnet test tests/AppointmentScheduler.Api.Tests/AppointmentScheduler.Api.Tests.csproj` (17 tests: 8 methods, one of which is a theory with 10 data rows).

| Area | AC | Tests |
|---|---|---|
| Successful booking | 1 | `Book_ValidAppointment_CreatesAndReturnsAppointment` |
| Overlap rejection | 2 | `Book_OverlappingActiveAppointmentForSameProvider_ReturnsConflictWithoutMutation` |
| Adjacency (half-open intervals) | 2 | `Book_AdjacentAppointments_AreAllowed` |
| Provider isolation | 2 | `Book_DifferentProviders_DoNotConflict` |
| Cancellation frees the slot / rebooking | 3 | `Cancel_Appointment_FreesItsInterval` |
| Unknown and already-cancelled IDs | 4 | `Cancel_MissingAndAlreadyCancelledIds_ReturnDistinctResultsWithoutUnexpectedMutation` |
| Invalid input, no mutation | 4 | `Book_InvalidRequests_DoNotChangeStoredAppointments`, 10 data rows: null, empty and whitespace provider; null start; start without UTC marker; non-zero offset; unparseable start; unparseable end; end equal to start; end before start |
| Concurrent booking | 5 | `Book_ConcurrentOverlappingRequests_AtMostOneSucceeds` |

AC 6 (clean build, passing tests) is verified by running `dotnet build` and `dotnet test`. AC 7 (AI use record) is a documentation deliverable, recorded in [ai-use-record.md](ai-use-record.md).

Acceptance criteria are defined in [requirements.md](requirements.md).

## Live HTTP checks (manual, not automated)

Run against the Development API and documented in [setup-and-run.md](setup-and-run.md):

- Swagger UI returns 200 and the OpenAPI document lists both appointment routes.
- Booking returns 201, a same-provider overlap returns 409, cancelling returns 200, and rebooking returns 201.
- A timestamp without the UTC marker returns 400 `invalid_input`.
- `DELETE /api/appointments` without an ID returns 400 `invalid_id`, and a `null` POST body returns 400 `invalid_input`.

## Known gaps

- Tests call the service directly. The HTTP layer (status code and error body mapping in `Program.cs`) is checked only by the manual live requests above, not by automated endpoint tests.
- The concurrency test repeats a contended scenario but cannot prove the absence of races. The guarantee is single-process only, from one in-process lock (see [design-spec.md](design-spec.md)).
- The in-memory store has no persistence, so persistence behavior is not tested (see [limitations.md](limitations.md)).
- Case-sensitive provider ID comparison and `+00:00` offset acceptance are documented rules without a dedicated test.
