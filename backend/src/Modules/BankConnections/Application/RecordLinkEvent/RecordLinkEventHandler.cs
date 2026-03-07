using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.RecordLinkEvent;

public sealed class RecordLinkEventHandler(
    ILinkEventRepository linkEventRepository,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(
        RecordLinkEventCommand command,
        CancellationToken cancellationToken)
    {
        var linkEvent = LinkEvent.Create(
            command.Institution,
            command.Region,
            command.Outcome,
            command.DurationMs,
            command.ErrorCategory,
            timeProvider.GetUtcNow());

        await linkEventRepository.AddAsync(linkEvent, cancellationToken);
    }
}
