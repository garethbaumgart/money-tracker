using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Contracts;
using MoneyTracker.Api.Diagnostics;
using MoneyTracker.Modules.Auth.Presentation;
using MoneyTracker.Modules.Budgets.Presentation;
using MoneyTracker.Modules.Households.Presentation;
using MoneyTracker.Modules.Transactions.Presentation;
using MoneyTracker.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatedConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthModule();
builder.Services.AddHouseholdsModule();
builder.Services.AddBudgetsModule();
builder.Services.AddTransactionsModule();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi("/openapi/v1.json");
}

app.MapGet("/", static () => Results.Ok(new { message = "MoneyTracker API" }));
app.MapGet("/health", static () => Results.Ok(new HealthResponse("ok")));
app.MapAuthEndpoints();
app.MapHouseholdEndpoints();
app.MapBudgetEndpoints();
app.MapBudgetSnapshotEndpoints();
app.MapTransactionEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", static IResult () => throw new InvalidOperationException("Simulated test failure."));
}

app.Run();

public partial class Program;
