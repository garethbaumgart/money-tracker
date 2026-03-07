namespace MoneyTracker.Modules.BankConnections.Domain;

public static class BankConnectionErrors
{
    public const string ValidationError = "validation_error";
    public const string ConnectionHouseholdNotFound = "bank_connection_household_not_found";
    public const string ConnectionAccessDenied = "bank_connection_access_denied";
    public const string ConnectionNotFound = "bank_connection_not_found";
    public const string ConnectionInvalidStateTransition = "bank_connection_invalid_state_transition";
    public const string ConnectionProviderError = "bank_connection_provider_error";
    public const string ConnectionCallbackInvalid = "bank_connection_callback_invalid";
    public const string ConnectionSessionExpired = "bank_connection_session_expired";
}
