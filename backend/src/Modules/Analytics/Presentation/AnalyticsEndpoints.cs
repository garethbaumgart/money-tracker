using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Analytics.Application.GenerateWeeklyReport;
using MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;
using MoneyTracker.Modules.Analytics.Application.GetFunnelReport;
using MoneyTracker.Modules.Analytics.Application.GetRetentionCohorts;
using MoneyTracker.Modules.Analytics.Application.GetRevenueMetrics;
using MoneyTracker.Modules.Analytics.Application.RecordEvent;
using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.Analytics.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Health;
using MoneyTracker.Modules.SharedKernel.Presentation;
using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Analytics.Presentation;

public static class AnalyticsEndpoints
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddSingleton<IActivationEventRepository, InMemoryActivationEventRepository>();
        services.AddSingleton<IAnalyticsEventPublisher, AnalyticsEventPublisher>();
        services.AddScoped<RecordEventHandler>();
        services.AddScoped<GetActivationFunnelHandler>();
        services.AddSingleton<IUserDataExportParticipant, AnalyticsDataExportParticipant>();
        services.AddSingleton<IUserDeletionParticipant, AnalyticsDataExportParticipant>();
        services.AddSingleton<IModuleHealthCheck, AnalyticsModuleHealthCheck>();

        // Funnel dashboard services (Issue #81)
        services.AddSingleton<IWeeklyReportRepository, InMemoryWeeklyReportRepository>();
        services.AddScoped<IFunnelDataSource, FunnelDataAggregator>();
        services.AddScoped<IRetentionDataSource, RetentionCalculator>();
        services.AddScoped<IRevenueDataSource, RevenueCalculator>();
        services.AddScoped<GetFunnelReportHandler>();
        services.AddScoped<GetRetentionCohortsHandler>();
        services.AddScoped<GetRevenueMetricsHandler>();
        services.AddScoped<GenerateWeeklyReportHandler>();
        services.AddHostedService<WeeklyReportWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var recordEventsEndpoint = (RouteHandlerBuilder)app.MapPost("/analytics/events", RecordEvents);
        recordEventsEndpoint
            .WithName("RecordAnalyticsEvents")
            .WithSummary("Record activation events.")
            .WithDescription("Accepts a batch of activation milestone events for funnel tracking.")
            .Accepts<RecordEventsRequest>("application/json")
            .Produces<RecordEventsResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var funnelEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/activation-funnel", GetActivationFunnel);
        funnelEndpoint
            .WithName("GetActivationFunnel")
            .WithSummary("Get activation funnel metrics.")
            .WithDescription("Returns ordered funnel stages with conversion and drop-off rates. Admin access required.")
            .Produces<ActivationFunnelResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var funnelReportEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/funnel-report", GetFunnelReport);
        funnelReportEndpoint
            .WithName("GetFunnelReport")
            .WithSummary("Get funnel report with trends and drop-off analysis.")
            .WithDescription("Returns funnel stages, top drop-offs, and WoW/MoM trends. Admin access required.")
            .Produces<FunnelReportResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var retentionEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/retention-cohorts", GetRetentionCohorts);
        retentionEndpoint
            .WithName("GetRetentionCohorts")
            .WithSummary("Get retention cohort data.")
            .WithDescription("Returns D1/D7/D14/D30 retention rates by signup week. Admin access required.")
            .Produces<RetentionCohortsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var revenueEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/revenue-metrics", GetRevenueMetrics);
        revenueEndpoint
            .WithName("GetRevenueMetrics")
            .WithSummary("Get revenue metrics.")
            .WithDescription("Returns MRR, ARPU, churn rate, and estimated LTV. Admin access required.")
            .Produces<RevenueMetricsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task RecordEvents(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<RecordEventsRequest>(httpContext, AnalyticsErrors.ValidationError);
        if (!isValidRequest || request is null)
        {
            if (parseProblem is not null)
            {
                await parseProblem.ExecuteAsync(httpContext);
            }
            else
            {
                await EndpointHelpers.WriteProblemAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "The request payload is required.",
                    AnalyticsErrors.ValidationError,
                    AnalyticsErrors.ValidationError);
            }
            return;
        }

        var platform = httpContext.Request.Headers["X-Platform"].FirstOrDefault() ?? "unknown";

        var handler = httpContext.RequestServices.GetRequiredService<RecordEventHandler>();
        var result = await handler.HandleAsync(
            new RecordEventCommand(
                authResult.AuthenticatedUser!.UserId,
                platform,
                request.Events
                    .Select(e => new RecordEventItem(
                        e.Milestone,
                        e.HouseholdId,
                        e.Metadata,
                        e.OccurredAtUtc))
                    .ToArray()),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                AnalyticsErrors.InvalidMilestone => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                AnalyticsErrors.ValidationError);
            return;
        }

        var response = new RecordEventsResponse(result.AcceptedCount, result.DuplicateCount);
        httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        await httpContext.Response.WriteAsJsonAsync(response, httpContext.RequestAborted);
    }

    private static async Task GetActivationFunnel(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        // Admin role gate
        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(
            authResult.AuthenticatedUser!.UserId,
            httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view activation funnel.",
                AnalyticsErrors.AccessDenied,
                AnalyticsErrors.AccessDenied);
            return;
        }

        var periodDaysRaw = httpContext.Request.Query["periodDays"].FirstOrDefault();
        var periodDays = 30;
        if (periodDaysRaw is not null && int.TryParse(periodDaysRaw, out var parsed) && parsed > 0)
        {
            periodDays = parsed;
        }

        var platform = httpContext.Request.Query["platform"].FirstOrDefault() ?? "all";
        var region = httpContext.Request.Query["region"].FirstOrDefault() ?? "all";

        var handler = httpContext.RequestServices.GetRequiredService<GetActivationFunnelHandler>();
        var result = await handler.HandleAsync(
            new GetActivationFunnelQuery(periodDays, platform, region),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                AnalyticsErrors.ValidationError);
            return;
        }

        var response = new ActivationFunnelResponse(
            result.PeriodDays,
            result.Platform,
            result.Region,
            result.TotalUsers,
            result.Stages
                .Select(s => new FunnelStageResponse(s.Milestone, s.UserCount, s.ConversionRate, s.DropOffRate))
                .ToArray(),
            result.Cohorts
                .Select(c => new CohortSummaryResponse(c.CohortKey, c.SignupCount, c.PaidConversionRate))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetFunnelReport(HttpContext httpContext)
    {
        if (!await RequireAdminAsync(httpContext, "Admin access is required to view funnel report."))
        {
            return;
        }

        var periodStartRaw = httpContext.Request.Query["periodStart"].FirstOrDefault();
        var periodEndRaw = httpContext.Request.Query["periodEnd"].FirstOrDefault();

        if (!DateTimeOffset.TryParse(periodStartRaw, out var periodStart)
            || !DateTimeOffset.TryParse(periodEndRaw, out var periodEnd))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Both periodStart and periodEnd query parameters are required in ISO 8601 format.",
                AnalyticsErrors.ValidationError,
                AnalyticsErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetFunnelReportHandler>();
        var result = await handler.HandleAsync(
            new GetFunnelReportQuery(periodStart, periodEnd),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                AnalyticsErrors.ValidationError);
            return;
        }

        var report = result.Report!;
        var response = new FunnelReportResponse(
            report.PeriodStart,
            report.PeriodEnd,
            report.Stages
                .Select(s => new FunnelReportStageResponse(s.Name, s.Count, s.ConversionRate, s.DropOffRate))
                .ToArray(),
            report.OverallConversion,
            report.TopDropOffs
                .Select(d => new DropOffResponse(d.FromStage, d.ToStage, d.DropOffRate, d.LostUsers))
                .ToArray(),
            new TrendsResponse(report.Trends.WeekOverWeek, report.Trends.MonthOverMonth));

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetRetentionCohorts(HttpContext httpContext)
    {
        if (!await RequireAdminAsync(httpContext, "Admin access is required to view retention cohorts."))
        {
            return;
        }

        var cohortCountRaw = httpContext.Request.Query["cohortCount"].FirstOrDefault();
        var cohortCount = 8;
        if (cohortCountRaw is not null && int.TryParse(cohortCountRaw, out var parsed) && parsed > 0)
        {
            cohortCount = parsed;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetRetentionCohortsHandler>();
        var result = await handler.HandleAsync(
            new GetRetentionCohortsQuery(cohortCount),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                AnalyticsErrors.ValidationError);
            return;
        }

        var response = new RetentionCohortsResponse(
            result.Cohorts
                .Select(c => new CohortRetentionResponse(c.Week, c.Signups, c.D1, c.D7, c.D14, c.D30))
                .ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetRevenueMetrics(HttpContext httpContext)
    {
        if (!await RequireAdminAsync(httpContext, "Admin access is required to view revenue metrics."))
        {
            return;
        }

        var asOfRaw = httpContext.Request.Query["asOf"].FirstOrDefault();
        DateTimeOffset? asOf = null;
        if (asOfRaw is not null && DateTimeOffset.TryParse(asOfRaw, out var parsed))
        {
            asOf = parsed;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetRevenueMetricsHandler>();
        var result = await handler.HandleAsync(
            new GetRevenueMetricsQuery(asOf),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                AnalyticsErrors.ValidationError);
            return;
        }

        var metrics = result.Metrics!;
        var response = new RevenueMetricsResponse(
            metrics.Mrr,
            metrics.Arpu,
            metrics.ChurnRate,
            metrics.EstimatedLtv,
            metrics.ActiveSubscribers,
            metrics.TrialUsers,
            new TrendsResponse(metrics.Trends.WeekOverWeek, metrics.Trends.MonthOverMonth));

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task<bool> RequireAdminAsync(HttpContext httpContext, string accessDeniedMessage)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return false;
        }

        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(
            authResult.AuthenticatedUser!.UserId,
            httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                accessDeniedMessage,
                AnalyticsErrors.AccessDenied,
                AnalyticsErrors.AccessDenied);
            return false;
        }

        return true;
    }
}

// Request DTOs
public sealed record RecordEventsRequest(RecordEventItemRequest[] Events);

public sealed record RecordEventItemRequest(
    string Milestone,
    Guid? HouseholdId,
    Dictionary<string, string>? Metadata,
    DateTimeOffset OccurredAtUtc);

// Response DTOs
public sealed record RecordEventsResponse(int AcceptedCount, int DuplicateCount);

public sealed record ActivationFunnelResponse(
    int PeriodDays,
    string Platform,
    string Region,
    int TotalUsers,
    FunnelStageResponse[] Stages,
    CohortSummaryResponse[] Cohorts);

public sealed record FunnelStageResponse(
    string Milestone,
    int UserCount,
    double ConversionRate,
    double DropOffRate);

public sealed record CohortSummaryResponse(
    string CohortKey,
    int SignupCount,
    double PaidConversionRate);

// Funnel report response DTOs
public sealed record FunnelReportResponse(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    FunnelReportStageResponse[] Stages,
    double OverallConversion,
    DropOffResponse[] TopDropOffs,
    TrendsResponse Trends);

public sealed record FunnelReportStageResponse(
    string Name,
    int Count,
    double ConversionRate,
    double DropOffRate);

public sealed record DropOffResponse(
    string FromStage,
    string ToStage,
    double DropOffRate,
    int LostUsers);

public sealed record TrendsResponse(
    double? WeekOverWeek,
    double? MonthOverMonth);

// Retention response DTOs
public sealed record RetentionCohortsResponse(
    CohortRetentionResponse[] Cohorts);

public sealed record CohortRetentionResponse(
    string Week,
    int Signups,
    double? D1,
    double? D7,
    double? D14,
    double? D30);

// Revenue response DTOs
public sealed record RevenueMetricsResponse(
    decimal Mrr,
    decimal Arpu,
    double ChurnRate,
    decimal? EstimatedLtv,
    int ActiveSubscribers,
    int TrialUsers,
    TrendsResponse Trends);
