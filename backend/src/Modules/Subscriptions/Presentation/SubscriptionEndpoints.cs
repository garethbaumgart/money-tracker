using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Subscriptions.Application.CheckFeatureAccess;
using MoneyTracker.Modules.Subscriptions.Application.GetEntitlements;
using MoneyTracker.Modules.Subscriptions.Application.GetSubscription;
using MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Modules.Subscriptions.Presentation;

public static class SubscriptionEndpoints
{
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services)
    {
        services.AddSingleton<ISubscriptionRepository, Infrastructure.InMemorySubscriptionRepository>();
        services.AddSingleton<IRevenueCatWebhookSignatureValidator, Infrastructure.RevenueCatWebhookSignatureValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ProcessRevenueCatWebhookHandler>();
        services.AddScoped<GetSubscriptionHandler>();

        services.AddSingleton<Infrastructure.SubscriptionEntitlementService>();
        services.AddSingleton<Domain.ISubscriptionEntitlementService>(sp =>
            sp.GetRequiredService<Infrastructure.SubscriptionEntitlementService>());
        services.AddSingleton<SharedKernel.Subscriptions.ISubscriptionEntitlementService>(sp =>
            sp.GetRequiredService<Infrastructure.SubscriptionEntitlementService>());
        services.AddScoped<GetEntitlementsHandler>();
        services.AddScoped<CheckFeatureAccessHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var webhookEndpoint = (RouteHandlerBuilder)app.MapPost("/webhooks/revenuecat", ProcessRevenueCatWebhook);
        webhookEndpoint
            .WithName("ProcessRevenueCatWebhook")
            .WithSummary("Receive RevenueCat webhook.")
            .WithDescription("Validates webhook signature and processes subscription lifecycle events.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        var entitlementsEndpoint = (RouteHandlerBuilder)app.MapGet("/subscriptions/entitlements", GetEntitlements);
        entitlementsEndpoint
            .WithName("GetEntitlements")
            .WithSummary("Get entitlements for a household.")
            .WithDescription("Returns the subscription tier and enabled feature keys for the specified household.")
            .Produces<EntitlementsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task ProcessRevenueCatWebhook(HttpContext httpContext)
    {
        // Read raw body for signature validation
        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(httpContext.RequestAborted);
        httpContext.Request.Body.Position = 0;

        var signature = httpContext.Request.Headers["X-RevenueCat-Signature"].FirstOrDefault() ?? string.Empty;

        // Try to parse the body for event metadata
        string? eventType = null;
        string? appUserId = null;
        string? productId = null;
        string? eventId = null;
        DateTimeOffset? periodStartUtc = null;
        DateTimeOffset? periodEndUtc = null;
        DateTimeOffset? originalPurchaseDateUtc = null;
        DateTimeOffset? cancelledAtUtc = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                var payload = JsonSerializer.Deserialize<RevenueCatWebhookPayload>(rawBody);
                var evt = payload?.Event;
                if (evt is not null)
                {
                    eventType = evt.Type;
                    appUserId = evt.AppUserId;
                    productId = evt.ProductId;
                    eventId = evt.Id;

                    if (evt.PeriodStartMs is not null)
                    {
                        periodStartUtc = DateTimeOffset.FromUnixTimeMilliseconds(evt.PeriodStartMs.Value);
                    }
                    if (evt.PeriodEndMs is not null)
                    {
                        periodEndUtc = DateTimeOffset.FromUnixTimeMilliseconds(evt.PeriodEndMs.Value);
                    }
                    if (evt.OriginalPurchaseDateMs is not null)
                    {
                        originalPurchaseDateUtc = DateTimeOffset.FromUnixTimeMilliseconds(evt.OriginalPurchaseDateMs.Value);
                    }
                    if (evt.CancellationDateMs is not null)
                    {
                        cancelledAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(evt.CancellationDateMs.Value);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore parse errors — signature validation will still run
        }

        var handler = httpContext.RequestServices.GetRequiredService<ProcessRevenueCatWebhookHandler>();
        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                signature,
                rawBody,
                eventType,
                appUserId,
                productId,
                eventId,
                periodStartUtc,
                periodEndUtc,
                originalPurchaseDateUtc,
                cancelledAtUtc),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                SubscriptionErrors.WebhookInvalidSignature => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                SubscriptionErrors.ValidationError);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task GetEntitlements(HttpContext httpContext)
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
                SubscriptionErrors.ValidationError,
                SubscriptionErrors.ValidationError);
            return;
        }

        var handler = httpContext.RequestServices.GetRequiredService<GetEntitlementsHandler>();
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(householdId, authResult.AuthenticatedUser!.UserId),
            httpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                SubscriptionErrors.HouseholdNotFound => StatusCodes.Status404NotFound,
                SubscriptionErrors.AccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status403Forbidden ? "Access denied." : "Validation failed.",
                result.ErrorMessage ?? "Request rejected.",
                result.ErrorCode,
                SubscriptionErrors.ValidationError);
            return;
        }

        var response = new EntitlementsResponse(
            result.Tier!,
            result.FeatureKeys!,
            result.TrialExpiresAtUtc,
            result.CurrentPeriodEndUtc);
        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static bool TryGetGuidQuery(HttpContext httpContext, string key, out Guid value)
    {
        value = Guid.Empty;
        var raw = httpContext.Request.Query[key].FirstOrDefault();
        return raw is not null && Guid.TryParse(raw, out value);
    }
}

internal sealed record RevenueCatWebhookPayload
{
    [JsonPropertyName("event")]
    public RevenueCatEvent? Event { get; init; }
}

internal sealed record RevenueCatEvent
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("app_user_id")]
    public string? AppUserId { get; init; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("period_start_ms")]
    public long? PeriodStartMs { get; init; }

    [JsonPropertyName("period_end_ms")]
    public long? PeriodEndMs { get; init; }

    [JsonPropertyName("original_purchase_date_ms")]
    public long? OriginalPurchaseDateMs { get; init; }

    [JsonPropertyName("cancellation_date_ms")]
    public long? CancellationDateMs { get; init; }
}

public sealed record EntitlementsResponse(
    string Tier,
    string[] FeatureKeys,
    DateTimeOffset? TrialExpiresAtUtc,
    DateTimeOffset? CurrentPeriodEndUtc);
