using System.Globalization;

namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record CohortKey(string Value)
{
    public static CohortKey FromDate(DateTimeOffset date)
    {
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var week = calendar.GetWeekOfYear(
            date.UtcDateTime,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
        var year = date.Year;

        // Handle edge case where the last days of the year belong to week 1 of the next year
        if (week == 1 && date.Month == 12)
        {
            year += 1;
        }

        return new CohortKey($"{year}-W{week:D2}");
    }
}
