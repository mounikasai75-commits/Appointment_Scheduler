using AppointmentScheduler.Api.Appointments;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AppointmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.SerializeAsV2 = true);
    app.UseSwaggerUI();
}

app.MapGet("/api/appointments", (AppointmentService service) =>
        Results.Ok(service.GetAppointments()))
    .WithName("ListAppointments")
    .WithOpenApi();

app.MapPost("/api/appointments", (CreateAppointmentRequest? request, AppointmentService service) =>
    {
        if (request is null)
        {
            return Results.BadRequest(new ApiError("invalid_input", "A booking request body is required."));
        }

        var result = service.Book(request.ProviderId, request.StartUtc, request.EndUtc);
        IResult response;

        if (result.Outcome == BookingOutcome.Booked)
        {
            response = Results.Created($"/api/appointments/{result.Appointment!.Id}", result.Appointment);
        }
        else if (result.Outcome == BookingOutcome.Conflict)
        {
            response = Results.Conflict(new ApiError(
                "conflict",
                "This provider already has an overlapping active appointment."));
        }
        else
        {
            response = Results.BadRequest(new ApiError(
                "invalid_input",
                "Provide a provider ID, valid UTC timestamps, and an end time after the start time."));
        }

        return response;
    })
    .WithName("CreateAppointment")
    .WithOpenApi()
    .Produces<Appointment>(StatusCodes.Status201Created)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .Produces<ApiError>(StatusCodes.Status409Conflict);

app.MapDelete("/api/appointments", () =>
        Results.BadRequest(new ApiError("invalid_id", "An appointment ID is required.")))
    .WithName("CancelAppointmentMissingId")
    .WithOpenApi()
    .Produces<ApiError>(StatusCodes.Status400BadRequest);

app.MapDelete("/api/appointments/{appointmentId}", (string appointmentId, AppointmentService service) =>
    {
        if (!Guid.TryParse(appointmentId, out var parsedAppointmentId))
        {
            return Results.BadRequest(new ApiError("invalid_id", "A valid appointment ID is required."));
        }

        var outcome = service.Cancel(parsedAppointmentId);
        IResult response;

        if (outcome == CancellationOutcome.Cancelled)
        {
            response = Results.Ok(new CancellationResponse("Appointment cancelled."));
        }
        else if (outcome == CancellationOutcome.NotFound)
        {
            response = Results.NotFound(new ApiError("not_found", "Appointment was not found."));
        }
        else if (outcome == CancellationOutcome.AlreadyCancelled)
        {
            response = Results.Conflict(new ApiError("already_cancelled", "Appointment was already cancelled."));
        }
        else
        {
            response = Results.BadRequest(new ApiError("invalid_id", "A non-empty appointment ID is required."));
        }

        return response;
    })
    .WithName("CancelAppointment")
    .WithOpenApi()
    .Produces<CancellationResponse>(StatusCodes.Status200OK)
    .Produces<ApiError>(StatusCodes.Status400BadRequest)
    .Produces<ApiError>(StatusCodes.Status404NotFound)
    .Produces<ApiError>(StatusCodes.Status409Conflict);

app.Run();

public partial class Program;
