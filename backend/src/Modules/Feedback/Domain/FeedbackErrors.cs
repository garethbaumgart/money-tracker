namespace MoneyTracker.Modules.Feedback.Domain;

public static class FeedbackErrors
{
    public const string ValidationError = "feedback_validation_error";
    public const string NotFound = "feedback_not_found";
    public const string InvalidStatusTransition = "feedback_invalid_status_transition";
    public const string NpsScoreOutOfRange = "feedback_nps_score_out_of_range";
    public const string RateLimitExceeded = "feedback_rate_limit_exceeded";
    public const string AccessDenied = "feedback_access_denied";
    public const string SummaryQueryFailed = "feedback_summary_query_failed";
    public const string CrashSummaryQueryFailed = "feedback_crash_summary_query_failed";
}
