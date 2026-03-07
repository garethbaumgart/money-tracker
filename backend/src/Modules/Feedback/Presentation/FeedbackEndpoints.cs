using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Feedback.Application.GetCrashSummary;
using MoneyTracker.Modules.Feedback.Application.GetFeedbackSummary;
using MoneyTracker.Modules.Feedback.Application.SubmitFeedback;
using MoneyTracker.Modules.Feedback.Application.SubmitNps;
using MoneyTracker.Modules.Feedback.Application.TriageFeedback;
using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Feedback.Presentation;

public static class FeedbackEndpoints
{
    public static IServiceCollection AddFeedbackModule(this IServiceCollection services)
    {
        services.AddSingleton<IFeedbackRepository, Infrastructure.InMemoryFeedbackRepository>();
        services.AddSingleton<INpsRepository, Infrastructure.InMemoryNpsRepository>();
        services.AddSingleton<ICrashReportRepository, Infrastructure.InMemoryCrashReportRepository>();
        services.AddSingleton<IGitHubIssueCreator, Infrastructure.NoOpGitHubIssueCreator>();
        services.AddSingleton<ISupportTicketService, Infrastructure.NoOpSupportTicketService>();
        services.AddSingleton<Infrastructure.PriorityScorer>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<SubmitFeedbackHandler>();
        services.AddScoped<SubmitNpsHandler>();
        services.AddScoped<GetFeedbackSummaryHandler>();
        services.AddScoped<GetCrashSummaryHandler>();
        services.AddScoped<TriageFeedbackHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var submitFeedbackEndpoint = (RouteHandlerBuilder)app.MapPost("/feedback", SubmitFeedback);
        submitFeedbackEndpoint
            .WithName("SubmitFeedback")
            .WithSummary("Submit user feedback.")
            .WithDescription("Submits feedback with category, description, rating, and device metadata.")
            .Accepts<SubmitFeedbackRequest>("application/json")
            .Produces<SubmitFeedbackResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        var submitNpsEndpoint = (RouteHandlerBuilder)app.MapPost("/feedback/nps", SubmitNps);
        submitNpsEndpoint
            .WithName("SubmitNps")
            .WithSummary("Submit NPS score.")
            .WithDescription("Records a Net Promoter Score (0-10) with optional comment.")
            .Accepts<SubmitNpsRequest>("application/json")
            .Produces<SubmitNpsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var feedbackSummaryEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/feedback-summary", GetFeedbackSummary);
        feedbackSummaryEndpoint
            .WithName("GetFeedbackSummary")
            .WithSummary("Get feedback summary metrics.")
            .WithDescription("Returns aggregated feedback metrics for a time period. Admin access required.")
            .Produces<FeedbackSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var crashSummaryEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/crash-summary", GetCrashSummary);
        crashSummaryEndpoint
            .WithName("GetCrashSummary")
            .WithSummary("Get crash summary metrics.")
            .WithDescription("Returns crash-free rate and top crash reports. Admin access required.")
            .Produces<CrashSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var triageEndpoint = (RouteHandlerBuilder)app.MapMethods("/admin/feedback/{id}/triage", ["PATCH"], TriageFeedback);
        triageEndpoint
            .WithName("TriageFeedback")
            .WithSummary("Triage feedback item.")
            .WithDescription("Updates feedback status and optionally overrides priority. Admin access required.")
            .Accepts<TriageFeedbackRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task SubmitFeedback(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<SubmitFeedbackRequest>(httpContext, FeedbackErrors.ValidationError);
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
                    FeedbackErrors.ValidationError,
                    FeedbackErrors.ValidationError);
            }
            return;
        }

        if (!Enum.TryParse<FeedbackCategory>(request.Category, ignoreCase: true, out var category))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Invalid category. Must be Bug, Feature, or General.",
                FeedbackErrors.ValidationError,
                FeedbackErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<SubmitFeedbackHandler>();
        var result = await handler.HandleAsync(
            new SubmitFeedbackCommand(
                authResult.AuthenticatedUser!.UserId,
                category,
                request.Description ?? string.Empty,
                request.Rating,
                request.ScreenName,
                request.AppVersion,
                request.DeviceModel,
                request.OsVersion,
                request.UserTier ?? "free"),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                FeedbackErrors.RateLimitExceeded => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status429TooManyRequests ? "Rate limit exceeded." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                FeedbackErrors.ValidationError);
            return;
        }

        var response = new SubmitFeedbackResponse(result.FeedbackId!.Value, result.Status!);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task SubmitNps(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<SubmitNpsRequest>(httpContext, FeedbackErrors.ValidationError);
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
                    FeedbackErrors.ValidationError,
                    FeedbackErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<SubmitNpsHandler>();
        var result = await handler.HandleAsync(
            new SubmitNpsCommand(
                authResult.AuthenticatedUser!.UserId,
                request.Score,
                request.Comment),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                FeedbackErrors.ValidationError);
            return;
        }

        var response = new SubmitNpsResponse(result.NpsId!.Value);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetFeedbackSummary(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        // AC-10: Admin role gate
        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view feedback summary.",
                FeedbackErrors.AccessDenied,
                FeedbackErrors.AccessDenied);
            return;
        }

        var periodDaysRaw = httpContext.Request.Query["periodDays"].FirstOrDefault();
        var periodDays = 30;
        if (periodDaysRaw is not null && int.TryParse(periodDaysRaw, out var parsed) && parsed > 0)
        {
            periodDays = parsed;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var periodStart = nowUtc.AddDays(-periodDays);

        var handler = httpContext.RequestServices.GetRequiredService<GetFeedbackSummaryHandler>();
        var result = await handler.HandleAsync(
            new GetFeedbackSummaryQuery(periodStart, nowUtc),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                FeedbackErrors.SummaryQueryFailed);
            return;
        }

        var data = result.Data!;
        var response = new FeedbackSummaryResponse(
            data.TotalFeedback,
            data.ByCategory,
            data.AvgSatisfaction,
            data.NpsScore,
            data.PriorityDistribution,
            data.Trends is not null
                ? new TrendResponse(
                    data.Trends.CurrentWeekCount,
                    data.Trends.PreviousWeekCount,
                    data.Trends.WowChangePercent)
                : null);

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetCrashSummary(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        // AC-10: Admin role gate
        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view crash summary.",
                FeedbackErrors.AccessDenied,
                FeedbackErrors.AccessDenied);
            return;
        }

        var periodDaysRaw = httpContext.Request.Query["periodDays"].FirstOrDefault();
        var periodDays = 30;
        if (periodDaysRaw is not null && int.TryParse(periodDaysRaw, out var parsed) && parsed > 0)
        {
            periodDays = parsed;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetCrashSummaryHandler>();
        var result = await handler.HandleAsync(
            new GetCrashSummaryQuery(periodDays),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                FeedbackErrors.CrashSummaryQueryFailed);
            return;
        }

        var data = result.Data!;
        var response = new CrashSummaryResponse(
            data.CrashFreeRate,
            data.TotalCrashes,
            data.TopCrashes.Select(c => new CrashReportResponse(
                c.Signature,
                c.Count,
                c.AffectedUsers,
                c.FirstSeen,
                c.LastSeen)).ToArray());

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task TriageFeedback(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        // AC-10: Admin role gate
        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to triage feedback.",
                FeedbackErrors.AccessDenied,
                FeedbackErrors.AccessDenied);
            return;
        }

        if (!TryGetGuidRoute(httpContext, "id", out var feedbackIdGuid))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "id route parameter is required.",
                FeedbackErrors.ValidationError,
                FeedbackErrors.ValidationError);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<TriageFeedbackRequest>(httpContext, FeedbackErrors.ValidationError);
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
                    FeedbackErrors.ValidationError,
                    FeedbackErrors.ValidationError);
            }
            return;
        }

        if (!Enum.TryParse<FeedbackStatus>(request.Status, ignoreCase: true, out var status))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "Invalid status. Must be Triaged, Resolved, or Dismissed.",
                FeedbackErrors.ValidationError,
                FeedbackErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<TriageFeedbackHandler>();
        var result = await handler.HandleAsync(
            new TriageFeedbackCommand(
                new FeedbackId(feedbackIdGuid),
                status,
                request.PriorityOverride),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                FeedbackErrors.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                FeedbackErrors.ValidationError);
            return;
        }

        await TypedResults.Ok(new { triaged = true }).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidRoute(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.RouteValues[key]?.ToString();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

public sealed record SubmitFeedbackRequest(
    string? Category,
    string? Description,
    int Rating,
    string? ScreenName,
    string? AppVersion,
    string? DeviceModel,
    string? OsVersion,
    string? UserTier);

public sealed record SubmitFeedbackResponse(Guid FeedbackId, string Status);

public sealed record SubmitNpsRequest(int Score, string? Comment);

public sealed record SubmitNpsResponse(Guid NpsId);

public sealed record TriageFeedbackRequest(string? Status, double? PriorityOverride);

public sealed record FeedbackSummaryResponse(
    int TotalFeedback,
    Dictionary<string, int> ByCategory,
    double AvgSatisfaction,
    double NpsScore,
    Dictionary<string, int> PriorityDistribution,
    TrendResponse? Trends);

public sealed record TrendResponse(
    int CurrentWeekCount,
    int PreviousWeekCount,
    double WowChangePercent);

public sealed record CrashSummaryResponse(
    double CrashFreeRate,
    int TotalCrashes,
    CrashReportResponse[] TopCrashes);

public sealed record CrashReportResponse(
    string Signature,
    int Count,
    int AffectedUsers,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen);
