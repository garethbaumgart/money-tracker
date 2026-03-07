using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class AnalyticsDataExportParticipant(IActivationEventRepository repository) : IUserDataExportParticipant, IUserDeletionParticipant
{
    public async Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        var allEvents = await repository.GetAllAsync(ct);
        var userEvents = allEvents
            .Where(e => e.UserId == userId)
            .Select(e => new
            {
                Milestone = e.Milestone.ToString(),
                e.HouseholdId,
                e.Platform,
                e.RecordedAtUtc
            })
            .ToArray();

        return new { activationEvents = userEvents };
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Analytics events will be anonymized or deleted during purge phase.
        return Task.CompletedTask;
    }
}
