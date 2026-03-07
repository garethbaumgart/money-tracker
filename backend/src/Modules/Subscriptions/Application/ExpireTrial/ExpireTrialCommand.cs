namespace MoneyTracker.Modules.Subscriptions.Application.ExpireTrial;

public sealed record ExpireTrialCommand(DateTimeOffset AsOfUtc);
