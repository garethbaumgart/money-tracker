using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Insights.Application.GetBudgetHealth;
using MoneyTracker.Modules.Insights.Application.GetSpendingSummary;
using MoneyTracker.Modules.Insights.Domain;
using MoneyTracker.Modules.SharedKernel.Health;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Insights.Presentation;

public static class InsightsEndpoints
{
    public static IServiceCollection AddInsightsModule(this IServiceCollection services)
    {
        services.AddScoped<GetSpendingSummaryHandler>();
        services.AddScoped<GetBudgetHealthHandler>();
        services.AddSingleton<IModuleHealthCheck, Infrastructure.InsightsModuleHealthCheck>();
        return services;
    }

    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var spendingSummaryEndpoint = (RouteHandlerBuilder)app.MapGet("/insights/spending-summary", GetSpendingSummary);
        spendingSummaryEndpoint
            .WithName("GetSpendingSummary")
            .WithSummary("Get spending summary with period-over-period comparison.")
            .WithDescription("Returns spending analysis for a household over a specified period with anomaly detection.")
            .Produces<SpendingSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var budgetHealthEndpoint = (RouteHandlerBuilder)app.MapGet("/insights/budget-health", GetBudgetHealth);
        budgetHealthEndpoint
            .WithName("GetBudgetHealth")
            .WithSummary("Get budget health score.")
            .WithDescription("Returns a composite budget health score with adherence, velocity, and bill payment components.")
            .Produces<BudgetHealthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task GetSpendingSummary(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidQuery(httpContext, "householdId", out var householdId))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "householdId query parameter is required.",
                InsightsErrors.ValidationError,
                InsightsErrors.ValidationError);
            return;
        }

        var periodRaw = httpContext.Request.Query["period"].FirstOrDefault();
        if (!InsightsPeriodExtensions.TryParse(periodRaw, out var period))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "period query parameter must be one of: 7d, 30d, 90d.",
                InsightsErrors.ValidationError,
                InsightsErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetSpendingSummaryHandler>();
        var result = await handler.HandleAsync(
            new GetSpendingSummaryQuery(householdId, authResult.AuthenticatedUser!.UserId, period),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                InsightsErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                InsightsErrors.AccessDenied => StatusCodes.Status403Forbidden,
                InsightsErrors.PremiumRequired => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                InsightsErrors.ValidationError);
            return;
        }

        var analysis = result.Analysis!;
        var response = new SpendingSummaryResponse(
            householdId,
            periodRaw!,
            result.PeriodStartUtc!.Value,
            result.PeriodEndUtc!.Value,
            analysis.TotalSpent,
            analysis.PreviousPeriodTotalSpent,
            analysis.SpendingChangePercent,
            analysis.Categories
                .Select(c => new CategorySpendingResponse(
                    c.CategoryId, c.CategoryName, c.CurrentSpent, c.PreviousSpent, c.ChangePercent))
                .ToArray(),
            analysis.Anomalies
                .Select(a => new SpendingAnomalyResponse(
                    a.CategoryId, a.CategoryName, a.CurrentSpent, a.PreviousSpent, a.ChangePercent))
                .ToArray(),
            analysis.TopCategories
                .Select(t => new TopCategoryResponse(
                    t.CategoryId, t.CategoryName, t.Amount, t.PercentOfTotal))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetBudgetHealth(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidQuery(httpContext, "householdId", out var householdId))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "householdId query parameter is required.",
                InsightsErrors.ValidationError,
                InsightsErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetBudgetHealthHandler>();
        var result = await handler.HandleAsync(
            new GetBudgetHealthQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                InsightsErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                InsightsErrors.AccessDenied => StatusCodes.Status403Forbidden,
                InsightsErrors.PremiumRequired => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                InsightsErrors.ValidationError);
            return;
        }

        var health = result.HealthScore!;
        var response = new BudgetHealthResponse(
            householdId,
            result.PeriodStartUtc!.Value,
            result.PeriodEndUtc!.Value,
            health.OverallScore,
            new ScoreBreakdownResponse(
                health.AdherenceScore, 0.40,
                health.VelocityScore, 0.35,
                health.BillPaymentScore, 0.25),
            health.CategoryHealth
                .Select(c => new CategoryHealthResponse(
                    c.CategoryId, c.CategoryName, c.Allocated, c.Spent, c.Status.ToString()))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

// --- Response DTOs ---

public sealed record SpendingSummaryResponse(
    Guid HouseholdId,
    string Period,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal TotalSpent,
    decimal PreviousPeriodTotalSpent,
    double SpendingChangePercent,
    CategorySpendingResponse[] Categories,
    SpendingAnomalyResponse[] Anomalies,
    TopCategoryResponse[] TopCategories);

public sealed record CategorySpendingResponse(
    Guid CategoryId,
    string CategoryName,
    decimal CurrentSpent,
    decimal PreviousSpent,
    double ChangePercent);

public sealed record SpendingAnomalyResponse(
    Guid CategoryId,
    string CategoryName,
    decimal CurrentSpent,
    decimal PreviousSpent,
    double ChangePercent);

public sealed record TopCategoryResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    double PercentOfTotal);

public sealed record BudgetHealthResponse(
    Guid HouseholdId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    int OverallScore,
    ScoreBreakdownResponse ScoreBreakdown,
    CategoryHealthResponse[] CategoryHealth);

public sealed record ScoreBreakdownResponse(
    double AdherenceScore,
    double AdherenceWeight,
    double VelocityScore,
    double VelocityWeight,
    double BillPaymentScore,
    double BillPaymentWeight);

public sealed record CategoryHealthResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Allocated,
    decimal Spent,
    string Status);
