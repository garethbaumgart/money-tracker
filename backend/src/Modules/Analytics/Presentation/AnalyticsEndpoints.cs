using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;
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
