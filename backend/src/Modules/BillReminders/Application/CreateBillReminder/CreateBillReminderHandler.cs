using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.CreateBillReminder;

public sealed class CreateBillReminderHandler(
    IBillReminderRepository repository,
    IHouseholdRepository householdRepository,
    TimeProvider timeProvider)
{
    public async Task<CreateBillReminderResult> HandleAsync(
        CreateBillReminderCommand command,
        CancellationToken cancellationToken)
    {
        var household = await householdRepository.GetByIdAsync(
            new HouseholdId(command.HouseholdId),
            cancellationToken);
        if (household is null)
        {
            return CreateBillReminderResult.HouseholdNotFound();
        }

        if (!household.IsMember(command.RequestingUserId))
        {
            return CreateBillReminderResult.AccessDenied();
        }

        BillReminder reminder;
        try
        {
            reminder = BillReminder.Create(
                command.HouseholdId,
                command.RequestingUserId,
                command.Title,
                command.Amount,
                command.DueDateUtc,
                command.Cadence,
                timeProvider.GetUtcNow());
        }
        catch (BillReminderDomainException exception)
        {
            return CreateBillReminderResult.Validation(exception.Code, exception.Message);
        }

        await repository.AddAsync(reminder, cancellationToken);
        return CreateBillReminderResult.Success(reminder);
    }
}
