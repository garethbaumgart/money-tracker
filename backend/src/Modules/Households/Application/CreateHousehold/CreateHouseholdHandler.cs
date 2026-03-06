using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.CreateHousehold;

public sealed class CreateHouseholdHandler(IHouseholdRepository repository, TimeProvider timeProvider)
{
    public async Task<CreateHouseholdResult> HandleAsync(CreateHouseholdCommand command, CancellationToken cancellationToken)
    {
        Household household;

        try
        {
            household = Household.Create(command.Name, timeProvider.GetUtcNow());
        }
        catch (HouseholdDomainException exception) when (exception.Code == HouseholdErrors.ValidationError)
        {
            return CreateHouseholdResult.Validation(exception.Message);
        }

        var exists = await repository.ExistsByNameAsync(household.Name, cancellationToken);
        if (exists)
        {
            return CreateHouseholdResult.Conflict();
        }

        await repository.AddAsync(household, cancellationToken);

        return CreateHouseholdResult.Success(household);
    }
}
