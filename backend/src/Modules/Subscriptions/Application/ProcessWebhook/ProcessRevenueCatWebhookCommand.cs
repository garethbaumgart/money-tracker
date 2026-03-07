namespace MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;

public sealed record ProcessRevenueCatWebhookCommand(
    string Signature,
    string RawBody,
    string? EventType,
    string? AppUserId,
    string? ProductId,
    string? EventId,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    DateTimeOffset? OriginalPurchaseDateUtc,
    DateTimeOffset? CancelledAtUtc);
