namespace MoneyTracker.Modules.Budgets.Domain;

public static class BudgetPeriod
{
    public static DateTimeOffset GetPeriodStart(DateTimeOffset nowUtc)
    {
        var utc = nowUtc.ToUniversalTime();
        return new DateTimeOffset(
            new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    public static DateTimeOffset GetPeriodEnd(DateTimeOffset periodStartUtc)
    {
        return periodStartUtc.AddMonths(1);
    }

    public static bool IsPeriodStart(DateTimeOffset periodStartUtc)
    {
        var utc = periodStartUtc.ToUniversalTime();
        return utc.Day == 1 && utc.TimeOfDay == TimeSpan.Zero;
    }
}
