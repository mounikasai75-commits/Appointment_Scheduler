namespace AppointmentScheduler.Api.Appointments;

public sealed record Appointment(
    Guid Id,
    string ProviderId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsCancelled = false);

public sealed record CreateAppointmentRequest(
    string? ProviderId,
    string? StartUtc,
    string? EndUtc);

public sealed record ApiError(string Code, string Message);

public sealed record CancellationResponse(string Message);

public enum BookingOutcome
{
    Booked,
    Conflict,
    InvalidInput
}

public sealed record BookingResult(BookingOutcome Outcome, Appointment? Appointment = null);

public enum CancellationOutcome
{
    Cancelled,
    NotFound,
    AlreadyCancelled,
    InvalidId
}