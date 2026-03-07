using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

public sealed class SubscriptionDataExportParticipant : IUserDataExportParticipant, IUserDeletionParticipant
{
    public Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Subscriptions are household-scoped (tied to householdId), not user-scoped.
        return Task.FromResult<object>(new { note = "Subscription data is household-scoped." });
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Subscriptions are household-scoped and not deleted with user.
        return Task.CompletedTask;
    }
}
