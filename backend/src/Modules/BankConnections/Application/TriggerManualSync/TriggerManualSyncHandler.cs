using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.BankConnections.Application.TriggerManualSync;

public sealed class TriggerManualSyncHandler(
    IHouseholdAccessService householdAccessService,
    SyncTransactionsHandler syncHandler)
{
    public async Task<SyncTransactionsResult> HandleAsync(
        TriggerManualSyncCommand command,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);

        if (!access.HouseholdExists)
        {
            return SyncTransactionsResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return SyncTransactionsResult.AccessDenied();
        }

        return await syncHandler.HandleAsync(
            new SyncTransactionsCommand(command.HouseholdId),
            cancellationToken);
    }
}
