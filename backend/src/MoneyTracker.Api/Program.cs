using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Contracts;
using MoneyTracker.Api.Diagnostics;
using MoneyTracker.Modules.Households.Presentation;
using MoneyTracker.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatedConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHouseholdsModule();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.MapGet("/", static () => Results.Ok(new { message = "MoneyTracker API" }));
app.MapGet("/health", static () => Results.Ok(new HealthResponse("ok")));
app.MapHouseholdEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", static IResult () => throw new InvalidOperationException("Simulated test failure."));
}

app.Run();

public partial class Program;
