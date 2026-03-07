using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.RecordLinkEvent;

public sealed record RecordLinkEventCommand(
    string Institution,
    string Region,
    EventOutcome Outcome,
    long DurationMs,
    string? ErrorCategory);
