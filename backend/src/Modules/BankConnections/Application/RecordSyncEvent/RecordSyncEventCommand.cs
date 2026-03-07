using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.RecordSyncEvent;

public sealed record RecordSyncEventCommand(
    Guid ConnectionId,
    string Institution,
    string Region,
    EventOutcome Outcome,
    long DurationMs,
    int TransactionCount,
    string? ErrorCategory);
