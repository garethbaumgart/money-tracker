using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.RecordEvent;

public sealed class RecordEventHandler(
    IActivationEventRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RecordEventResult> HandleAsync(
        RecordEventCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Events.Count == 0)
        {
            return RecordEventResult.Failure(
                AnalyticsErrors.ValidationError,
                "At least one event is required.");
        }

        var acceptedCount = 0;
        var duplicateCount = 0;
        var recordedAtUtc = timeProvider.GetUtcNow();

        foreach (var item in command.Events)
        {
            if (!ActivationMilestoneExtensions.TryParse(item.Milestone, out var milestone))
            {
                return RecordEventResult.Failure(
                    AnalyticsErrors.InvalidMilestone,
                    $"Invalid milestone: '{item.Milestone}'.");
            }

            var exists = await repository.ExistsAsync(
                command.UserId,
                milestone,
                cancellationToken);

            if (exists)
            {
                duplicateCount += 1;
                continue;
            }

            var activationEvent = ActivationEvent.Create(
                command.UserId,
                milestone,
                item.HouseholdId,
                command.Platform,
                region: null,
                item.Metadata,
                item.OccurredAtUtc,
                recordedAtUtc);

            await repository.AddAsync(activationEvent, cancellationToken);
            acceptedCount += 1;
        }

        return RecordEventResult.Success(acceptedCount, duplicateCount);
    }
}
