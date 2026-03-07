namespace MoneyTracker.Modules.Insights.Domain;

public enum InsightsPeriod
{
    SevenDays,
    ThirtyDays,
    NinetyDays
}

public static class InsightsPeriodExtensions
{
    public static int ToDays(this InsightsPeriod period) => period switch
    {
        InsightsPeriod.SevenDays => 7,
        InsightsPeriod.ThirtyDays => 30,
        InsightsPeriod.NinetyDays => 90,
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    public static bool TryParse(string? value, out InsightsPeriod period)
    {
        period = InsightsPeriod.ThirtyDays;

        return value switch
        {
            "7d" => SetAndReturn(InsightsPeriod.SevenDays, ref period),
            "30d" => SetAndReturn(InsightsPeriod.ThirtyDays, ref period),
            "90d" => SetAndReturn(InsightsPeriod.NinetyDays, ref period),
            _ => false
        };
    }

    private static bool SetAndReturn(InsightsPeriod value, ref InsightsPeriod period)
    {
        period = value;
        return true;
    }
}
