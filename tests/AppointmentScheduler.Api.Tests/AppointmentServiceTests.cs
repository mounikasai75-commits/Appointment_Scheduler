using AppointmentScheduler.Api.Appointments;

namespace AppointmentScheduler.Api.Tests;

public sealed class AppointmentServiceTests
{
    [Fact]
    public void Book_ValidAppointment_CreatesAndReturnsAppointment()
    {
        var service = new AppointmentService();

        var result = service.Book("provider-a", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z");

        Assert.Equal(BookingOutcome.Booked, result.Outcome);
        Assert.NotNull(result.Appointment);
        Assert.NotEqual(Guid.Empty, result.Appointment.Id);
        Assert.Equal("provider-a", result.Appointment.ProviderId);
        Assert.Equal(new[] { result.Appointment }, service.GetAppointments());
    }

    [Fact]
    public void Book_OverlappingActiveAppointmentForSameProvider_ReturnsConflictWithoutMutation()
    {
        var service = new AppointmentService();
        var existing = Book(service, "provider-a", 9, 10);

        var result = service.Book("provider-a", "2026-10-24T09:30:00Z", "2026-10-24T10:30:00Z");

        Assert.Equal(BookingOutcome.Conflict, result.Outcome);
        Assert.Equal(new[] { existing }, service.GetAppointments());
    }

    [Fact]
    public void Book_AdjacentAppointments_AreAllowed()
    {
        var service = new AppointmentService();
        Book(service, "provider-a", 9, 10);

        var result = service.Book("provider-a", "2026-10-24T10:00:00Z", "2026-10-24T11:00:00Z");

        Assert.Equal(BookingOutcome.Booked, result.Outcome);
        Assert.Equal(2, service.GetAppointments().Count);
    }

    [Fact]
    public void Book_DifferentProviders_DoNotConflict()
    {
        var service = new AppointmentService();
        Book(service, "provider-a", 9, 10);

        var result = service.Book("provider-b", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z");

        Assert.Equal(BookingOutcome.Booked, result.Outcome);
        Assert.Equal(2, service.GetAppointments().Count);
    }

    [Fact]
    public void Cancel_Appointment_FreesItsInterval()
    {
        var service = new AppointmentService();
        var appointment = Book(service, "provider-a", 9, 10);

        Assert.Equal(CancellationOutcome.Cancelled, service.Cancel(appointment.Id));
        Assert.Equal(BookingOutcome.Booked, service.Book("provider-a", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z").Outcome);
        Assert.True(service.GetAppointments().Single(item => item.Id == appointment.Id).IsCancelled);
    }

    [Fact]
    public void Cancel_MissingAndAlreadyCancelledIds_ReturnDistinctResultsWithoutUnexpectedMutation()
    {
        var service = new AppointmentService();
        var appointment = Book(service, "provider-a", 9, 10);

        Assert.Equal(CancellationOutcome.NotFound, service.Cancel(Guid.NewGuid()));
        Assert.Equal(new[] { appointment }, service.GetAppointments());
        Assert.Equal(CancellationOutcome.InvalidId, service.Cancel(Guid.Empty));
        Assert.Equal(new[] { appointment }, service.GetAppointments());

        Assert.Equal(CancellationOutcome.Cancelled, service.Cancel(appointment.Id));
        var cancelled = appointment with { IsCancelled = true };
        Assert.Equal(new[] { cancelled }, service.GetAppointments());
        Assert.Equal(CancellationOutcome.AlreadyCancelled, service.Cancel(appointment.Id));
        Assert.Equal(new[] { cancelled }, service.GetAppointments());
    }

    [Theory]
    [InlineData(null, "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z")]
    [InlineData("", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z")]
    [InlineData("   ", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", null, "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", "2026-10-24T09:00:00", "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", "2026-10-24T09:00:00+01:00", "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", "not-a-timestamp", "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", "2026-10-24T09:00:00Z", "not-a-timestamp")]
    [InlineData("provider-a", "2026-10-24T10:00:00Z", "2026-10-24T10:00:00Z")]
    [InlineData("provider-a", "2026-10-24T11:00:00Z", "2026-10-24T10:00:00Z")]
    public void Book_InvalidRequests_DoNotChangeStoredAppointments(
        string? providerId,
        string? startUtc,
        string? endUtc)
    {
        var service = new AppointmentService();
        var existing = Book(service, "provider-a", 8, 9);

        var result = service.Book(providerId, startUtc, endUtc);

        Assert.Equal(BookingOutcome.InvalidInput, result.Outcome);
        Assert.Equal(new[] { existing }, service.GetAppointments());
    }

    [Fact]
    public async Task Book_ConcurrentOverlappingRequests_AtMostOneSucceeds()
    {
        var service = new AppointmentService();
        const int requestCount = 24;
        using var ready = new CountdownEvent(requestCount);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async _ =>
            {
                ready.Signal();
                await release.Task;
                return service.Book("provider-a", "2026-10-24T09:00:00Z", "2026-10-24T10:00:00Z");
            })
            .ToArray();

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Concurrent requests did not reach the start gate.");
        release.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Single(results, result => result.Outcome == BookingOutcome.Booked);
        Assert.Equal(requestCount - 1, results.Count(result => result.Outcome == BookingOutcome.Conflict));
        Assert.Single(service.GetAppointments());
    }

    private static Appointment Book(AppointmentService service, string providerId, int startHour, int endHour)
    {
        var result = service.Book(
            providerId,
            $"2026-10-24T{startHour:00}:00:00Z",
            $"2026-10-24T{endHour:00}:00:00Z");

        Assert.Equal(BookingOutcome.Booked, result.Outcome);
        return result.Appointment!;
    }
}