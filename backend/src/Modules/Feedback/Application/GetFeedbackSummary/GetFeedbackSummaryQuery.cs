namespace MoneyTracker.Modules.Feedback.Application.GetFeedbackSummary;

public sealed record GetFeedbackSummaryQuery(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);
