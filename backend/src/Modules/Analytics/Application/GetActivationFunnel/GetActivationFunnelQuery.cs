namespace MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;

public sealed record GetActivationFunnelQuery(
    int PeriodDays = 30,
    string Platform = "all",
    string Region = "all");
