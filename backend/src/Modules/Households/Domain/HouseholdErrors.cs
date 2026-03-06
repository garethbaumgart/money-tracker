namespace MoneyTracker.Modules.Households.Domain;

public static class HouseholdErrors
{
    public const string ValidationError = "validation_error";
    public const string HouseholdNameConflict = "household_name_conflict";
    public const string HouseholdNotFound = "household_not_found";
    public const string HouseholdAccessDenied = "household_access_denied";
    public const string HouseholdInvitationNotFound = "household_invitation_not_found";
    public const string HouseholdInvitationExpired = "household_invitation_expired";
    public const string HouseholdInvitationUsed = "household_invitation_used";
    public const string HouseholdInvitationEmailMismatch = "household_invitation_email_mismatch";
    public const string HouseholdAlreadyMember = "household_already_member";
}
