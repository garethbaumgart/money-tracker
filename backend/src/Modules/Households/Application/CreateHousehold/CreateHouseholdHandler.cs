using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.CreateHousehold;

public sealed class CreateHouseholdHandler(IHouseholdRepository repository, TimeProvider timeProvider)
{
    public async Task<CreateHouseholdResult> HandleAsync(CreateHouseholdCommand command, CancellationToken cancellationToken)
    {
        Household household;

        try
        {
            household = Household.Create(command.Name, command.OwnerUserId, timeProvider.GetUtcNow());
        }
        catch (HouseholdDomainException exception) when (exception.Code == HouseholdErrors.ValidationError)
        {
            return CreateHouseholdResult.Validation(exception.Message);
        }

        var added = await repository.AddIfNotExistsAsync(household, cancellationToken);
        if (!added)
        {
            return CreateHouseholdResult.Conflict();
        }

        return CreateHouseholdResult.Success(household);
    }
}
