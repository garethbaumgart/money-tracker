using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Subscriptions.Application.GetSubscription;
using MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;
using MoneyTracker.Modules.Subscriptions.Domain;
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
