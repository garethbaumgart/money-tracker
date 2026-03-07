using MoneyTracker.Api;
using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Contracts;
using MoneyTracker.Api.Diagnostics;
using MoneyTracker.Modules.Auth.Presentation;
using MoneyTracker.Modules.BillReminders.Presentation;
using MoneyTracker.Modules.Budgets.Presentation;
using MoneyTracker.Modules.Households.Presentation;
using MoneyTracker.Modules.Notifications.Presentation;
using MoneyTracker.Modules.Transactions.Presentation;
using MoneyTracker.Modules.BankConnections.Presentation;
using MoneyTracker.Modules.Subscriptions.Presentation;
using MoneyTracker.Modules.Analytics.Presentation;
using MoneyTracker.Modules.Insights.Presentation;
using MoneyTracker.Api.Health;
using MoneyTracker.Modules.Experiments.Presentation;
using MoneyTracker.Api.Observability;
using MoneyTracker.Api.Security;
using MoneyTracker.Modules.SharedKernel.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatedConfiguration(builder.Configuration);
builder.Services.AddSingleton<ErrorRateMonitor>();
builder.Services.AddSingleton<BackgroundWorkerHealthTracker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthModule();
builder.Services.AddSingleton<IModuleHealthCheck, AuthModuleHealthCheck>();
builder.Services.AddHouseholdsModule();
builder.Services.AddBudgetsModule();
builder.Services.AddTransactionsModule();
builder.Services.AddBillRemindersModule();
builder.Services.AddNotificationsModule();
builder.Services.AddBankConnectionsModule();
builder.Services.AddSubscriptionsModule();
builder.Services.AddAnalyticsModule();
builder.Services.AddInsightsModule();
builder.Services.AddExperimentsModule();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<PayloadSizeLimitMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
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
app.MapBillReminderEndpoints();
app.MapNotificationEndpoints();
app.MapBankConnectionEndpoints();
app.MapSubscriptionEndpoints();
app.MapAnalyticsEndpoints();
app.MapInsightsEndpoints();
app.MapExperimentEndpoints();
app.MapSystemHealthEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", static IResult () => throw new InvalidOperationException("Simulated test failure."));
}

app.Run();

public partial class Program;
