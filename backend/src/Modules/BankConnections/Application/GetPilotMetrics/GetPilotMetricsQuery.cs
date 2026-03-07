namespace MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;

public sealed record GetPilotMetricsQuery(int PeriodDays = 30, string? Region = null);
