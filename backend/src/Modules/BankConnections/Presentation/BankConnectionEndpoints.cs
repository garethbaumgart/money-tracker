using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;
using MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;
using MoneyTracker.Modules.BankConnections.Application.GetBankConnections;
using MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;
using MoneyTracker.Modules.BankConnections.Application.RecordLinkEvent;
using MoneyTracker.Modules.BankConnections.Application.RecordSyncEvent;
using MoneyTracker.Modules.BankConnections.Application.ReConsent;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Application.TriggerManualSync;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.BankConnections.Presentation;

public static class BankConnectionEndpoints
{
    public static IServiceCollection AddBankConnectionsModule(this IServiceCollection services)
    {
        services.AddSingleton<IBankConnectionRepository, Infrastructure.InMemoryBankConnectionRepository>();
        services.AddSingleton<IBankProviderAdapter, Infrastructure.InMemoryBankProviderAdapter>();
        services.AddSingleton<IWebhookSignatureValidator, Infrastructure.BasiqWebhookSignatureValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<Infrastructure.BasiqBankProviderAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://au-api.basiq.io");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<ISyncEventRepository, Infrastructure.InMemorySyncEventRepository>();
        services.AddSingleton<ILinkEventRepository, Infrastructure.InMemoryLinkEventRepository>();
        services.AddSingleton<IConsentNotificationSender, Infrastructure.InMemoryConsentNotificationSender>();
        services.AddScoped<CreateLinkSessionHandler>();
        services.AddScoped<ProcessCallbackHandler>();
        services.AddScoped<GetBankConnectionsHandler>();
        services.AddSingleton<SyncTransactionsHandler>();
        services.AddScoped<ProcessWebhookHandler>();
        services.AddScoped<TriggerManualSyncHandler>();
        services.AddScoped<RecordSyncEventHandler>();
        services.AddScoped<RecordLinkEventHandler>();
        services.AddScoped<GetPilotMetricsHandler>();
        services.AddSingleton<IAdminAccessService, ConfigurationAdminAccessService>();
        services.AddScoped<ReConsentHandler>();
        services.AddSingleton<CheckConsentExpiryHandler>();
        services.AddHostedService<Infrastructure.TransactionSyncWorker>();
        services.AddHostedService<Infrastructure.ConsentExpiryCheckWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapBankConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var createLinkSessionEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/link-session", CreateLinkSession);
        createLinkSessionEndpoint
            .WithName("CreateBankLinkSession")
            .WithSummary("Create a bank link session.")
            .WithDescription("Creates a Basiq consent session and returns a consent URL for the user.")
            .Accepts<CreateLinkSessionRequest>("application/json")
            .Produces<LinkSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var callbackEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/callback", ProcessCallback);
        callbackEndpoint
            .WithName("ProcessBankCallback")
            .WithSummary("Process bank connection callback.")
            .WithDescription("Handles the OAuth redirect callback from the bank provider.")
            .Accepts<ProcessCallbackRequest>("application/json")
            .Produces<BankConnectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var getConnectionsEndpoint = (RouteHandlerBuilder)app.MapGet("/bank/connections", GetBankConnections);
        getConnectionsEndpoint
            .WithName("GetBankConnections")
            .WithSummary("List bank connections.")
            .WithDescription("Returns all bank connections for a household with current status.")
            .Produces<BankConnectionsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var syncEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/sync", TriggerManualSync);
        syncEndpoint
            .WithName("TriggerManualSync")
            .WithSummary("Trigger manual transaction sync.")
            .WithDescription("Triggers an immediate sync for a household's active bank connections.")
            .Accepts<TriggerSyncRequest>("application/json")
            .Produces<SyncSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var reConsentEndpoint = (RouteHandlerBuilder)app.MapPost("/bank/connections/{connectionId}/re-consent", ReConsentConnection);
        reConsentEndpoint
            .WithName("ReConsentConnection")
            .WithSummary("Re-consent an expired or revoked connection.")
            .WithDescription("Generates a new Basiq consent URL for re-consenting an expired or revoked bank connection.")
            .Produces<ReConsentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var webhookEndpoint = (RouteHandlerBuilder)app.MapPost("/webhooks/basiq", ProcessBasiqWebhook);
        webhookEndpoint
            .WithName("ProcessBasiqWebhook")
            .WithSummary("Receive Basiq webhook.")
            .WithDescription("Validates webhook signature and triggers transaction sync or handles consent revocation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        var pilotMetricsEndpoint = (RouteHandlerBuilder)app.MapGet("/admin/pilot-metrics", GetPilotMetrics);
        pilotMetricsEndpoint
            .WithName("GetPilotMetrics")
            .WithSummary("Get pilot quality metrics.")
            .WithDescription("Returns aggregated sync, link, and consent metrics for the NZ fallback decision. Admin access required.")
            .Produces<PilotMetricsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task CreateLinkSession(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<CreateLinkSessionRequest>(httpContext, BankConnectionErrors.ValidationError);
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
                    BankConnectionErrors.ValidationError,
                    BankConnectionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<CreateLinkSessionHandler>();
        var result = await handler.HandleAsync(
            new CreateLinkSessionCommand(
                request.HouseholdId,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionHouseholdNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new LinkSessionResponse(result.ConsentUrl!, result.ConnectionId!.Value);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task ProcessCallback(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<ProcessCallbackRequest>(httpContext, BankConnectionErrors.ValidationError);
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
                    BankConnectionErrors.ValidationError,
                    BankConnectionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<ProcessCallbackHandler>();
        var result = await handler.HandleAsync(
            new ProcessCallbackCommand(request.ConsentSessionId),
            httpContext.RequestAborted);

        if (!result.IsSuccess && !result.IsFailedConnection)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionSessionExpired => StatusCodes.Status400BadRequest,
                BankConnectionErrors.ConnectionCallbackInvalid => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var connection = result.Connection!;

        // Verify the authenticated user has access to the connection's household
        var householdAccess = httpContext.RequestServices.GetRequiredService<IHouseholdAccessService>();
        var accessResult = await householdAccess.CheckMemberAsync(
            connection.HouseholdId,
            authResult.AuthenticatedUser!.UserId,
            httpContext.RequestAborted);

        if (!accessResult.IsMember)
        {
            var statusCode = accessResult.HouseholdExists
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status404NotFound;

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Not found.",
                statusCode == StatusCodes.Status403Forbidden
                    ? "You do not have access to this household."
                    : "The household was not found.",
                statusCode == StatusCodes.Status403Forbidden
                    ? BankConnectionErrors.ConnectionAccessDenied
                    : BankConnectionErrors.ConnectionHouseholdNotFound,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new BankConnectionResponse(
            connection.Id.Value,
            connection.HouseholdId,
            connection.InstitutionName,
            connection.Status.ToString(),
            connection.ConsentStatus.ToString(),
            connection.ConsentExpiresAtUtc,
            connection.ErrorCode,
            connection.ErrorMessage,
            connection.CreatedAtUtc,
            connection.UpdatedAtUtc);

        if (result.IsFailedConnection)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Connection failed.",
                result.ErrorMessage ?? "The bank connection could not be established.",
                result.ErrorCode,
                BankConnectionErrors.ConnectionProviderError);
            return;
        }

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task GetBankConnections(HttpContext httpContext)
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
                BankConnectionErrors.ValidationError,
                BankConnectionErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetBankConnectionsHandler>();
        var result = await handler.HandleAsync(
            new GetBankConnectionsQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionHouseholdNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new BankConnectionsResponse(
            result.Connections!
                .Select(c => new BankConnectionSummaryResponse(
                    c.Id,
                    c.HouseholdId,
                    c.InstitutionName,
                    c.Status,
                    c.ConsentStatus,
                    c.ConsentExpiresAtUtc,
                    c.ErrorCode,
                    c.ErrorMessage,
                    c.CreatedAtUtc,
                    c.UpdatedAtUtc))
                .ToArray());
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task TriggerManualSync(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var (isValidRequest, request, parseProblem) =
            await EndpointHelpers.ReadJsonRequestAsync<TriggerSyncRequest>(httpContext, BankConnectionErrors.ValidationError);
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
                    BankConnectionErrors.ValidationError,
                    BankConnectionErrors.ValidationError);
            }
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<TriggerManualSyncHandler>();
        var result = await handler.HandleAsync(
            new TriggerManualSyncCommand(
                request.HouseholdId,
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionHouseholdNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new SyncSummaryResponse(
            result.SyncedCount,
            result.SkippedCount,
            result.FailedConnections);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task ReConsentConnection(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        if (!TryGetGuidRoute(httpContext, "connectionId", out var connectionId))
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "connectionId route parameter is required.",
                BankConnectionErrors.ValidationError,
                BankConnectionErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<ReConsentHandler>();
        var result = await handler.HandleAsync(
            new ReConsentCommand(
                new BankConnectionId(connectionId),
                authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.ConnectionNotFound => StatusCodes.Status404NotFound,
                BankConnectionErrors.ConnectionAccessDenied => StatusCodes.Status403Forbidden,
                BankConnectionErrors.ReConsentNotNeeded => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        var response = new ReConsentResponse(result.ConsentUrl!);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task ProcessBasiqWebhook(HttpContext httpContext)
    {
        // Read raw body for signature validation
        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(httpContext.RequestAborted);
        httpContext.Request.Body.Position = 0;

        var signature = httpContext.Request.Headers["X-Basiq-Signature"].FirstOrDefault() ?? string.Empty;

        // Try to parse the body for event metadata
        string? eventType = null;
        string? connectionId = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                var payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody);
                eventType = payload?.EventType;
                connectionId = payload?.ConnectionId;
            }
        }
        catch (JsonException)
        {
            // Ignore parse errors — signature validation will still run
        }

        var handler = httpContext.RequestServices.GetRequiredService<ProcessWebhookHandler>();
        var result = await handler.HandleAsync(
            new ProcessWebhookCommand(signature, rawBody, eventType, connectionId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                BankConnectionErrors.WebhookInvalidSignature => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                BankConnectionErrors.ValidationError);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task GetPilotMetrics(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        // Admin role gate: authenticated user must have admin access.
        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(authResult.AuthenticatedUser!.UserId, httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view pilot metrics.",
                PilotMetricErrors.MetricsAccessDenied,
                PilotMetricErrors.MetricsAccessDenied);
            return;
        }

        var periodDaysRaw = httpContext.Request.Query["periodDays"].FirstOrDefault();
        var periodDays = 30;
        if (periodDaysRaw is not null && int.TryParse(periodDaysRaw, out var parsed) && parsed > 0)
        {
            periodDays = parsed;
        }

        var region = httpContext.Request.Query["region"].FirstOrDefault();

        var handler = httpContext.RequestServices.GetRequiredService<GetPilotMetricsHandler>();
        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(periodDays, region),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                PilotMetricErrors.MetricsAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Request failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                PilotMetricErrors.MetricsQueryFailed);
            return;
        }

        var response = new PilotMetricsResponse(
            result.PeriodDays,
            new SyncMetricsResponse(
                result.SyncMetrics!.OverallSuccessRate,
                result.SyncMetrics.ByRegion
                    .Select(r => new RegionSyncMetricResponse(r.Region, r.SuccessRate, r.AvgLatencyMs))
                    .ToArray(),
                result.SyncMetrics.ByInstitution
                    .Select(i => new InstitutionSyncMetricResponse(i.Institution, i.SuccessRate, i.AvgLatencyMs))
                    .ToArray()),
            new LinkMetricsResponse(
                result.LinkMetrics!.ByInstitution
                    .Select(i => new InstitutionLinkMetricResponse(i.Institution, i.Attempted, i.Successful))
                    .ToArray()),
            new ConsentHealthResponse(
                result.ConsentHealth!.AverageDurationDays,
                result.ConsentHealth.ReConsentRate,
                result.ConsentHealth.RevocationRate));

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
    }

    private static bool TryGetGuidRoute(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.RouteValues[key]?.ToString();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

public sealed record CreateLinkSessionRequest(Guid HouseholdId);

public sealed record ProcessCallbackRequest(string ConsentSessionId);

public sealed record LinkSessionResponse(string ConsentUrl, Guid ConnectionId);

public sealed record BankConnectionResponse(
    Guid Id,
    Guid HouseholdId,
    string? InstitutionName,
    string Status,
    string? ConsentStatus,
    DateTimeOffset? ConsentExpiresAtUtc,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BankConnectionSummaryResponse(
    Guid Id,
    Guid HouseholdId,
    string? InstitutionName,
    string Status,
    string? ConsentStatus,
    DateTimeOffset? ConsentExpiresAtUtc,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ReConsentResponse(string ConsentUrl);

public sealed record BankConnectionsResponse(BankConnectionSummaryResponse[] Connections);

public sealed record TriggerSyncRequest(Guid HouseholdId);

public sealed record SyncSummaryResponse(
    int SyncedCount,
    int SkippedCount,
    int FailedConnections);

internal sealed record WebhookPayload
{
    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; init; }
}

public sealed record PilotMetricsResponse(
    int PeriodDays,
    SyncMetricsResponse SyncMetrics,
    LinkMetricsResponse LinkMetrics,
    ConsentHealthResponse ConsentHealth);

public sealed record SyncMetricsResponse(
    double OverallSuccessRate,
    RegionSyncMetricResponse[] ByRegion,
    InstitutionSyncMetricResponse[] ByInstitution);

public sealed record RegionSyncMetricResponse(string Region, double SuccessRate, double AvgLatencyMs);

public sealed record InstitutionSyncMetricResponse(string Institution, double SuccessRate, double AvgLatencyMs);

public sealed record LinkMetricsResponse(InstitutionLinkMetricResponse[] ByInstitution);

public sealed record InstitutionLinkMetricResponse(string Institution, int Attempted, int Successful);

public sealed record ConsentHealthResponse(double AverageDurationDays, double ReConsentRate, double RevocationRate);
