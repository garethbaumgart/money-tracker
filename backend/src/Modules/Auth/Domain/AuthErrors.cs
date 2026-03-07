namespace MoneyTracker.Modules.Auth.Domain;

public static class AuthErrors
{
    public const string ValidationError = "validation_error";
    public const string ChallengeNotFound = "auth_challenge_not_found";
    public const string ChallengeExpired = "auth_challenge_expired";
    public const string ChallengeUsed = "auth_challenge_used";
    public const string ChallengeEmailMismatch = "auth_challenge_email_mismatch";
    public const string ChallengeAttemptLimitReached = "auth_challenge_attempt_limit_reached";
    public const string AccessTokenMissing = "auth_access_token_missing";
    public const string AccessTokenInvalid = "auth_access_token_invalid";
    public const string AccessTokenExpired = "auth_access_token_expired";
    public const string RefreshTokenInvalid = "auth_refresh_token_invalid";
    public const string RefreshTokenExpired = "auth_refresh_token_expired";
    public const string DataExportForbidden = "auth_data_export_forbidden";
    public const string DeleteForbidden = "auth_delete_forbidden";
}

public static class AuthPolicy
{
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    public const int MaxChallengeAttempts = 5;
}
