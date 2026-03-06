using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.CreateHousehold;

public sealed class CreateHouseholdResult
{
    private CreateHouseholdResult(Household? household, string? errorCode, string? errorMessage)
    {
        Household = household;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Household? Household { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Household is not null;

    public static CreateHouseholdResult Success(Household household)
    {
        return new CreateHouseholdResult(household, errorCode: null, errorMessage: null);
    }

    public static CreateHouseholdResult Validation(string message)
    {
        return new CreateHouseholdResult(household: null, HouseholdErrors.ValidationError, message);
    }

    public static CreateHouseholdResult Conflict()
    {
        return new CreateHouseholdResult(
            household: null,
            HouseholdErrors.HouseholdNameConflict,
            "A household with this name already exists.");
    }
}
