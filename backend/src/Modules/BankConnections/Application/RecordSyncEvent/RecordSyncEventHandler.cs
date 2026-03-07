using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.RecordSyncEvent;

public sealed class RecordSyncEventHandler(
    ISyncEventRepository syncEventRepository,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(
        RecordSyncEventCommand command,
        CancellationToken cancellationToken)
    {
        var syncEvent = SyncEvent.Create(
            command.ConnectionId,
            command.Institution,
            command.Region,
            command.Outcome,
            command.DurationMs,
            command.TransactionCount,
            command.ErrorCategory,
            timeProvider.GetUtcNow());

        await syncEventRepository.AddAsync(syncEvent, cancellationToken);
    }
}
