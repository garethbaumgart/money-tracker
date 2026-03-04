using MoneyTracker.Api.Contracts;
using MoneyTracker.Api.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", static () => Results.Ok(new { message = "MoneyTracker API" }));
app.MapGet("/health", static () => Results.Ok(new HealthResponse("ok")));

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", static IResult () => throw new InvalidOperationException("Simulated test failure."));
}

app.Run();

public partial class Program;
