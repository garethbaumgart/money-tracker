using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.GetBillReminders;

public sealed class GetBillRemindersHandler(
    IBillReminderRepository repository,
    IHouseholdRepository householdRepository)
{
    public async Task<GetBillRemindersResult> HandleAsync(
        GetBillRemindersQuery query,
        CancellationToken cancellationToken)
    {
        var household = await householdRepository.GetByIdAsync(
            new HouseholdId(query.HouseholdId),
            cancellationToken);
        if (household is null)
        {
            return GetBillRemindersResult.HouseholdNotFound();
        }

        if (!household.IsMember(query.RequestingUserId))
        {
            return GetBillRemindersResult.AccessDenied();
        }

        var reminders = await repository.GetByHouseholdAsync(query.HouseholdId, cancellationToken);
        var ordered = reminders.OrderBy(reminder => reminder.NextDueDateUtc).ToArray();
        return GetBillRemindersResult.Success(ordered);
    }
}
