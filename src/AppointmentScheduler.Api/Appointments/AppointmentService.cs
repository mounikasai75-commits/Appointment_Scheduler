using System.Globalization;

namespace AppointmentScheduler.Api.Appointments;

public sealed class AppointmentService
{
    private static readonly string[] UtcFormats =
    [
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"
    ];

    private readonly object _gate = new();
    private readonly Dictionary<Guid, Appointment> _appointments = new();

    public BookingResult Book(string? providerId, string? startUtc, string? endUtc)
    {
        if (string.IsNullOrWhiteSpace(providerId) ||
            !TryParseUtc(startUtc, out var parsedStartUtc) ||
            !TryParseUtc(endUtc, out var parsedEndUtc) ||
            parsedEndUtc <= parsedStartUtc)
        {
            return new BookingResult(BookingOutcome.InvalidInput);
        }

        lock (_gate)
        {
            var hasConflict = _appointments.Values.Any(existing =>
                !existing.IsCancelled &&
                string.Equals(existing.ProviderId, providerId, StringComparison.Ordinal) &&
                existing.StartUtc < parsedEndUtc &&
                parsedStartUtc < existing.EndUtc);

            if (hasConflict)
            {
                return new BookingResult(BookingOutcome.Conflict);
            }

            var appointment = new Appointment(Guid.NewGuid(), providerId, parsedStartUtc, parsedEndUtc);
            _appointments.Add(appointment.Id, appointment);
            return new BookingResult(BookingOutcome.Booked, appointment);
        }
    }

    public CancellationOutcome Cancel(Guid appointmentId)
    {
        if (appointmentId == Guid.Empty)
        {
            return CancellationOutcome.InvalidId;
        }

        lock (_gate)
        {
            if (!_appointments.TryGetValue(appointmentId, out var appointment))
            {
                return CancellationOutcome.NotFound;
            }

            if (appointment.IsCancelled)
            {
                return CancellationOutcome.AlreadyCancelled;
            }

            _appointments[appointmentId] = appointment with { IsCancelled = true };
            return CancellationOutcome.Cancelled;
        }
    }

    public IReadOnlyList<Appointment> GetAppointments()
    {
        lock (_gate)
        {
            return _appointments.Values
                .OrderBy(appointment => appointment.StartUtc)
                .ToArray();
        }
    }

    private static bool TryParseUtc(string? input, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (string.IsNullOrWhiteSpace(input) ||
            (!input.EndsWith("Z", StringComparison.Ordinal) &&
             !input.EndsWith("+00:00", StringComparison.Ordinal)))
        {
            return false;
        }

        var normalizedInput = input.EndsWith("Z", StringComparison.Ordinal)
            ? input[..^1] + "+00:00"
            : input;

        if (!DateTimeOffset.TryParseExact(
                normalizedInput,
                UtcFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed) ||
            parsed.Offset != TimeSpan.Zero)
        {
            return false;
        }

        timestamp = parsed.ToUniversalTime();
        return true;
    }
}